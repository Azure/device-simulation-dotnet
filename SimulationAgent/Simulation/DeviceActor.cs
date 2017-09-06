// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation
{
    public interface IDeviceActor
    {
        /// <summary>
        /// Azure IoT Hub client shared by Connect and SendTelemetry
        /// </summary>
        IDeviceClient Client { get; set; }

        /// <summary>
        /// Azure IoT Hub client used by DeviceBootstrap
        /// This extra client is required only because Device Twins require a
        /// MQTT connection. If the main client already uses MQTT, the logic
        /// won't open a new connection, and reuse the existing one instead.
        /// </summary>
        IDeviceClient BootstrapClient { get; set; }

        /// <summary>
        /// The virtual state of the simulated device. The state is
        /// periodically updated using an external script. The value
        /// is shared by UpdateDeviceState and SendTelemetry.
        /// </summary>
        Dictionary<string, object> DeviceState { get; set; }

        /// <summary>
        /// The status of this device simulation actor, e.g. whether
        /// it's connecting, connected, sending, etc. Each state machine
        /// step monitors this value.
        /// </summary>
        Status ActorStatus { get; }

        /// <summary>
        /// Token used to stop the simulation, monitored by the state machine
        /// steps.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Invoke this method before calling Start(), to initialize the actor
        /// with details like the device model and message type to simulate.
        /// If this method is not called before Start(), the application will
        /// thrown an exception.
        /// Setup() should be called only once, typically after the constructor.
        /// </summary>
        IDeviceActor Setup(DeviceModel deviceModel, int position);

        /// <summary>
        /// Call this method to start the simulated device, e.g. sending
        /// messages and responding to method calls.
        /// Pass a cancellation token, possibly the same to all the actors in
        /// the simulation, so it's easy to stop the entire simulation
        /// cancelling just one common token.
        /// </summary>
        void Start(CancellationToken cancellationToken);

        /// <summary>
        /// An optional method to stop the device actor, instead of using the
        /// cancellation token.
        /// </summary>
        void Stop();

        /// <summary>
        /// State machine flow. Change the internal state and schedule the
        /// execution of the new corresponding logic.
        /// </summary>
        void MoveNext();
    }

    public class DeviceActor : IDeviceActor
    {
        private const string DEVICE_ID_PREFIX = "Simulated.";

        // When the actor fails to connect to IoT Hub, it retries every 10 seconds
        private static readonly TimeSpan retryConnectingFrequency = TimeSpan.FromSeconds(10);

        // When the actor fails to bootstrap, it retries every 60 seconds - it is longer b/c in 
        // bootstrap we're registering methods which have a 10 second timeout apiece
        private static readonly TimeSpan retryBootstrappingFrequency = TimeSpan.FromSeconds(60);

        // When connecting or sending a message, timeout after 5 seconds
        private static readonly TimeSpan connectionTimeout = TimeSpan.FromSeconds(5);

        // Used to make sure the actor checks at least every 10 seconds
        // if the simulation needs to stop
        private static readonly TimeSpan checkCancellationFrequency = TimeSpan.FromSeconds(10);

        // A timer used for the action of the current state. It's used to
        // retry connecting, and to keep refreshing the device state.
        private readonly ITimer timer;

        // A collection of timers used to send messages. Each simulated device
        // can send multiple messages, with different frequency.
        private List<ITimer> telemetryTimers;

        // A timer used to check whether the simulation stopped and the actor
        // should stop running.
        private readonly ITimer cancellationCheckTimer;

        // How often the simulated device state needs to be updated, i.e.
        // when to execute the external script. The value is configured in
        // the device model.
        private TimeSpan deviceStateInterval;

        // Info about the messages to generate and send
        private IList<DeviceModel.DeviceModelMessage> messages;

        // ID of the simulated device, used with Azure IoT Hub
        private string deviceId;

        private readonly ILogger log;

        // DI factory used to instantiate timers
        private readonly DependencyResolution.IFactory factory;

        // Ensure that setup is called only once, to keep the actor thread safe
        private bool setupDone = false;

        // State machine logic, each of the following has a Run() method
        private readonly Connect connectLogic;

        private readonly UpdateDeviceState updateDeviceStateLogic;
        private readonly DeviceBootstrap deviceBootstrapLogic;
        private readonly SendTelemetry sendTelemetryLogic;

        /// <summary>
        /// Azure IoT Hub client shared by Connect and SendTelemetry
        /// </summary>
        public IDeviceClient Client { get; set; }

        /// <summary>
        /// Azure IoT Hub client used by DeviceBootstrap
        /// This extra client is required  because Device Twins and Device 
        /// Methods require an MQTT connection. If the main client already 
        /// uses MQTT, the logic won't open a new connection, and reuse the 
        /// existing one instead.
        /// </summary>
        public IDeviceClient BootstrapClient { get; set; }

        /// <summary>
        /// The virtual state of the simulated device. The state is
        /// periodically updated using an external script. The value
        /// is shared by UpdateDeviceState and SendTelemetry.
        /// </summary>
        public Dictionary<string, object> DeviceState { get; set; }

        /// <summary>
        /// The status of this device simulation actor, e.g. whether
        /// it's connecting, connected, sending, etc.
        /// </summary>
        public Status ActorStatus { get; private set; }

        /// <summary>
        /// Token used to stop the simulation, monitored by the state machine
        /// steps.
        /// </summary>
        public CancellationToken CancellationToken { get; private set; }

        public DeviceActor(
            ILogger logger,
            DependencyResolution.IFactory factory)
        {
            this.log = logger;
            this.deviceBootstrapLogic = factory.Resolve<DeviceBootstrap>();
            this.connectLogic = factory.Resolve<Connect>();
            this.updateDeviceStateLogic = factory.Resolve<UpdateDeviceState>();
            this.sendTelemetryLogic = factory.Resolve<SendTelemetry>();
            this.factory = factory;

            this.ActorStatus = Status.None;
            this.timer = this.factory.Resolve<ITimer>();
            this.cancellationCheckTimer = this.factory.Resolve<ITimer>();
            this.telemetryTimers = new List<ITimer>();
        }

        /// <summary>
        /// Invoke this method before calling Start(), to initialize the actor
        /// with details like the device model and message type to simulate.
        /// If this method is not called before Start(), the application will
        /// thrown an exception.
        /// Setup() should be called only once, typically after the constructor.
        /// </summary>
        public IDeviceActor Setup(DeviceModel deviceModel, int position)
        {
            if (this.ActorStatus != Status.None || this.setupDone)
            {
                this.log.Error("The actor is already initialized",
                    () => new
                    {
                        CurrentDeviceId = this.deviceId,
                        NewDeviceModelName = deviceModel.Name,
                        NewDeviceModelId = deviceModel.Id,
                        NewPosition = position
                    });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.setupDone = true;

            this.deviceId = DEVICE_ID_PREFIX + deviceModel.Id + "." + position;
            this.messages = deviceModel.Telemetry;

            this.deviceStateInterval = deviceModel.Simulation.Script.Interval;
            this.DeviceState = SetupTelemetryAndProperties(deviceModel);
            this.log.Debug("Initial device state", () => new { this.deviceId, this.DeviceState });

            this.connectLogic.Setup(this.deviceId, deviceModel);
            this.updateDeviceStateLogic.Setup(this.deviceId, deviceModel);
            this.deviceBootstrapLogic.Setup(this.deviceId, deviceModel);
            this.sendTelemetryLogic.Setup(this.deviceId, deviceModel);

            this.log.Debug("Setup complete", () => new { this.deviceId });
            this.MoveNext();

            return this;
        }

        private Dictionary<string, object> SetupTelemetryAndProperties(DeviceModel deviceModel)
        {
            // put telemetry properties in state
            Dictionary<string, object> state = CloneObject(deviceModel.Simulation.InitialState);

            //TODO: think about whether these should be pulled from the hub instead of disk
            //(the device model); i.e. what if someone has modified the hub twin directly
            // put reported properties from device model into state
            foreach (var property in deviceModel.Properties)
                state.Add(property.Key, property.Value);

            //TODO:This is used to control whether telemetry is calculated in UpdateDeviceState.  
            //methods can turn telemetry off/on; e.g. setting temp high- turnoff, set low, turn on
            //it would be better to do this at the telemetry item level - we should add this in the future
            state.Add("CalculateRandomizedTelemetry", true);

            return state;
        }

        /// <summary>
        /// Call this method to start the simulated device, e.g. sending
        /// messages and responding to method calls.
        /// Pass a cancellation token, possibly the same to all the actors in
        /// the simulation, so it's easy to stop the entire simulation
        /// cancelling just one common token.
        /// </summary>
        public void Start(CancellationToken token)
        {
            switch (this.ActorStatus)
            {
                default:
                    this.log.Debug("The actor already started", () => new { this.deviceId });
                    break;

                case Status.None:
                    this.log.Error("The actor is not initialized", () => { });
                    throw new DeviceActorNotInitializedException();

                case Status.Ready:
                    this.CancellationToken = token;
                    this.log.Debug("Starting...", () => new { this.deviceId });
                    this.MoveNext();
                    break;
            }
        }

        /// <summary>
        /// An optional method to stop the device actor, instead of using the
        /// cancellation token.
        /// </summary>
        public void Stop()
        {
            this.StopTimers();
            this.Client?.DisconnectAsync().Wait(connectionTimeout);
            this.BootstrapClient?.DisconnectAsync().Wait(connectionTimeout);
            this.ActorStatus = Status.Ready;
            this.log.Debug("Stopped", () => new { this.deviceId });
        }

        /// <summary>
        /// State machine flow. Change the internal state and schedule the
        /// execution of the new corresponding logic.
        /// </summary>
        public void MoveNext()
        {
            var nextStatus = this.ActorStatus + 1;
            this.log.Debug("Changing actor state to " + nextStatus,
                () => new { this.deviceId, ActorStatus = this.ActorStatus.ToString(), nextStatus = nextStatus.ToString() });

            switch (nextStatus)
            {
                case Status.Ready:
                    this.ActorStatus = nextStatus;
                    break;

                case Status.Connecting:
                    this.ActorStatus = nextStatus;
                    this.StopTimers();
                    this.log.Debug("Scheduling connectLogic", () => new { this.deviceId });
                    this.timer.Setup(this.connectLogic.Run, this, retryConnectingFrequency);
                    this.timer.Start();
                    this.ScheduleCancellationCheckIfRequired(retryConnectingFrequency);
                    break;

                case Status.BootstrappingDevice:
                    this.ActorStatus = nextStatus;
                    this.StopTimers();
                    this.log.Debug("Scheduling deviceBootstrapLogic", () => new { this.deviceId });
                    this.timer.Setup(this.deviceBootstrapLogic.Run, this, retryBootstrappingFrequency);
                    this.timer.Start();
                    this.ScheduleCancellationCheckIfRequired(retryBootstrappingFrequency);
                    break;

                case Status.UpdatingDeviceState:
                    this.ActorStatus = nextStatus;
                    this.StopTimers();
                    this.log.Debug("Scheduling updateDeviceStateLogic", () => new { this.deviceId });
                    this.timer.Setup(this.updateDeviceStateLogic.Run, this, this.deviceStateInterval);
                    this.timer.Start();
                    this.ScheduleCancellationCheckIfRequired(this.deviceStateInterval);
                    break;

                case Status.SendingTelemetry:
                    this.ActorStatus = nextStatus;
                    foreach (var message in this.messages)
                    {
                        var telemetryTimer = this.factory.Resolve<ITimer>();
                        this.telemetryTimers.Add(telemetryTimer);
                        var callContext = new SendTelemetryContext
                        {
                            Self = this,
                            Message = message
                        };

                        this.log.Debug("Scheduling sendTelemetryLogic", () =>
                            new { this.deviceId, message.Interval.TotalSeconds, message.MessageSchema.Name, message.MessageTemplate });

                        telemetryTimer.Setup(this.sendTelemetryLogic.Run, callContext, message.Interval);
                        telemetryTimer.StartIn(message.Interval);
                    }
                    break;
                default:
                    this.log.Error("Unknown next status",
                        () => new { this.deviceId, this.ActorStatus, nextStatus });
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// When the telemetry is not sentv very often, for example once
        /// every 5 minutes, this method is executed more frequently, to
        /// check whether the user has asked to stop the simulation.
        /// </summary>
        /// <param name="context">The actor instance (i.e. 'this')</param>
        private static void CancellationCheck(object context)
        {
            var self = (DeviceActor) context;
            if (self.CancellationToken.IsCancellationRequested)
            {
                self.Stop();
            }
        }

        /// <summary>
        /// Check whether a second timer is required, to periodically check if
        /// the user asks to stop the simulation. The extra timer is needed
        /// when the actor remains inactive for long periods, for example when
        /// sending telemetry every 5 minutes.
        /// </summary>
        private void ScheduleCancellationCheckIfRequired(TimeSpan curr)
        {
            if (curr > checkCancellationFrequency)
            {
                this.cancellationCheckTimer.Setup(CancellationCheck, this, checkCancellationFrequency);
                this.cancellationCheckTimer.Start();
            }
        }

        private void StopTimers()
        {
            this.log.Debug("Stopping timers", () => new { this.deviceId });
            this.timer.Stop();
            this.cancellationCheckTimer.Stop();

            foreach (var t in this.telemetryTimers) t.Stop();
            this.telemetryTimers = new List<ITimer>();
        }

        /// <summary>Copy an object by value</summary>
        private static T CloneObject<T>(T source)
        {
            return JsonConvert.DeserializeObject<T>(
                JsonConvert.SerializeObject(source));
        }

        private class TelemetryContext
        {
            public DeviceActor Self { get; set; }
            public DeviceModel.DeviceModelMessage Message { get; set; }
        }
    }
}
