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
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic.Models;
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
        // ID prefix of the simulated devices, used with Azure IoT Hub
        private const string DEVICE_ID_PREFIX = "Simulated.";

        private const string CALC_TELEMETRY = "CalculateRandomizedTelemetry";

        // Timeout when disconnecting a client
        private const int DISCONNECT_TIMEOUT_MSECS = 5000;

        // Checks every 10 seconds if the simulation needs to stop
        private const int CHECK_CANCELLATION_FREQUENCY_MSECS = 10000;

        // A timer used to check whether the simulation stopped and the actor
        // should stop running.
        private readonly ITimer cancellationCheckTimer;

        private readonly ILogger log;

        private string deviceId;

        // Ensure that setup is called only once, to keep the actor thread safe
        private bool setupDone = false;

        // ""State machine"" logic, each of the following tasks have a Run()
        // method, and some tasks can be active at the same time

        private readonly IDeviceStatusLogic connectLogic;
        private readonly IDeviceStatusLogic deviceBootstrapLogic;
        private readonly IDeviceStatusLogic updateDeviceStateLogic;
        private readonly IDeviceStatusLogic updateReportedPropertiesLogic;
        private readonly IDeviceStatusLogic sendTelemetryLogic;

        // Logic used to throttled the hub operations
        private readonly IRateLimiting rateLimiting;

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
            Connect connectLogic,
            DeviceBootstrap deviceBootstrapLogic,
            UpdateDeviceState updateDeviceStateLogic,
            UpdateReportedProperties updateReportedPropertiesLogic,
            SendTelemetry sendTelemetryLogic,
            IRateLimiting rateLimiting,
            ITimer cancellationCheckTimer)
        {
            this.log = logger;
            this.rateLimiting = rateLimiting;

            this.connectLogic = connectLogic;
            this.deviceBootstrapLogic = deviceBootstrapLogic;
            this.updateDeviceStateLogic = updateDeviceStateLogic;
            this.updateReportedPropertiesLogic = updateReportedPropertiesLogic;
            this.sendTelemetryLogic = sendTelemetryLogic;

            this.cancellationCheckTimer = cancellationCheckTimer;
            this.cancellationCheckTimer.Setup(CancellationCheck, this);

            this.ActorStatus = Status.None;
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

            this.DeviceState = this.SetupTelemetryAndProperties(deviceModel);
            this.log.Debug("Initial device state", () => new { this.deviceId, this.DeviceState });

            this.connectLogic.Setup(this.deviceId, deviceModel, this);
            this.updateDeviceStateLogic.Setup(this.deviceId, deviceModel, this);
            this.deviceBootstrapLogic.Setup(this.deviceId, deviceModel, this);
            this.sendTelemetryLogic.Setup(this.deviceId, deviceModel, this);
            this.updateReportedPropertiesLogic.Setup(this.deviceId, deviceModel, this);

            this.log.Debug("Setup complete", () => new { this.deviceId });
            this.MoveNext();

            return this;
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
                    this.rateLimiting.SetCancellationToken(token);
                    this.log.Debug("Starting...", () => new { this.deviceId });
                    this.cancellationCheckTimer.RunOnce(0);
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
            if (this.ActorStatus <= Status.Ready) return;

            this.ActorStatus = Status.None;

            // TODO: I see this not exiting cleanly sometimes in the logs (it throws)
            //       https://github.com/Azure/device-simulation-dotnet/issues/56

            this.log.Debug("Stopping device actor...", () => { });

            this.TryStopConnectLogic();
            this.TryStopDeviceBootstrapLogic();
            this.TryStopUpdateDeviceStateLogic();
            this.TryStopUpdateReportedPropertiesLogic();
            this.TryDisconnectBootstrapClient();
            this.TryDisconnectClient();

            this.ActorStatus = Status.Ready;
            this.log.Debug("Device actor stopped", () => new { this.deviceId });
        }

        /// <summary>
        /// State machine flow. Change the internal state and schedule the
        /// execution of the new corresponding logic.
        /// </summary>
        public void MoveNext()
        {
            var nextStatus = this.ActorStatus + 1;
            this.log.Debug("Changing device actor state to " + nextStatus,
                () => new { this.deviceId, ActorStatus = this.ActorStatus.ToString(), nextStatus = nextStatus.ToString() });

            switch (nextStatus)
            {
                case Status.Ready:
                    this.ActorStatus = nextStatus;
                    break;

                case Status.Connecting:
                    this.ActorStatus = nextStatus;
                    this.connectLogic.Start();
                    break;

                case Status.BootstrappingDevice:
                    this.ActorStatus = nextStatus;
                    this.connectLogic.Stop();
                    this.deviceBootstrapLogic.Start();
                    break;

                case Status.UpdatingDeviceState:
                    this.ActorStatus = nextStatus;
                    this.deviceBootstrapLogic.Stop();
                    this.updateDeviceStateLogic.Start();
                    break;

                case Status.UpdatingReportedProperties:
                    this.ActorStatus = nextStatus;
                    this.updateReportedPropertiesLogic.Start();
                    // Note: at this point both UpdatingDeviceState
                    //       and UpdatingReportedProperties should be running
                    break;

                case Status.SendingTelemetry:
                    this.ActorStatus = nextStatus;
                    this.sendTelemetryLogic.Start();
                    // Note: at this point
                    //       UpdatingDeviceState, UpdatingReportedProperties and SendingTelemetry
                    //       should be running
                    break;
                default:
                    this.log.Error("Unknown next status",
                        () => new { this.deviceId, this.ActorStatus, nextStatus });
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Dictionary<string, object> SetupTelemetryAndProperties(DeviceModel deviceModel)
        {
            // put telemetry properties in state
            Dictionary<string, object> state = CloneObject(deviceModel.Simulation.InitialState);

            // TODO: think about whether these should be pulled from the hub instead of disk
            // (the device model); i.e. what if someone has modified the hub twin directly
            // put reported properties from device model into state
            foreach (var property in deviceModel.Properties)
                state.Add(property.Key, property.Value);

            // TODO:This is used to control whether telemetry is calculated in UpdateDeviceState.
            // methods can turn telemetry off/on; e.g. setting temp high- turnoff, set low, turn on
            // it would be better to do this at the telemetry item level - we should add this in the future
            state.Add(CALC_TELEMETRY, true);

            return state;
        }

        /// <summary>Copy an object by value</summary>
        private static T CloneObject<T>(T source)
        {
            return JsonConvert.DeserializeObject<T>(
                JsonConvert.SerializeObject(source));
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
            if (self.CancellationToken.IsCancellationRequested && self.ActorStatus > Status.Ready)
            {
                self.Stop();
            }
            else
            {
                self.cancellationCheckTimer.RunOnce(CHECK_CANCELLATION_FREQUENCY_MSECS);
            }
        }

        private void TryDisconnectClient()
        {
            if (this.Client == null) return;

            try
            {
                this.log.Debug("Disconnecting device client", () => new { this.deviceId });
                this.Client.DisconnectAsync().Wait(DISCONNECT_TIMEOUT_MSECS);
            }
            catch (Exception e)
            {
                this.log.Error("Unable to disconnect the device client", () => new { e });
            }

            this.Client = null;
        }

        private void TryDisconnectBootstrapClient()
        {
            if (this.BootstrapClient == null) return;
            if (this.Client.Protocol == this.BootstrapClient.Protocol) return;

            try
            {
                this.log.Debug("Disconnecting device bootstrap client", () => new { this.deviceId });
                this.BootstrapClient.DisconnectAsync().Wait(DISCONNECT_TIMEOUT_MSECS);
            }
            catch (Exception e)
            {
                this.log.Error("Unable to disconnect the device boostrap client",
                    () => new { e });
            }

            this.BootstrapClient = null;
        }

        private void TryStopUpdateReportedPropertiesLogic()
        {
            try
            {
                this.updateReportedPropertiesLogic.Stop();
            }
            catch (Exception e)
            {
                this.log.Error("Unable to stop UpdateReportedProperties logic", () => new { e });
            }
        }

        private void TryStopUpdateDeviceStateLogic()
        {
            try
            {
                this.updateDeviceStateLogic.Stop();
            }
            catch (Exception e)
            {
                this.log.Error("Unable to stop UpdateDeviceState logic", () => new { e });
            }
        }

        private void TryStopDeviceBootstrapLogic()
        {
            try
            {
                this.deviceBootstrapLogic.Stop();
            }
            catch (Exception e)
            {
                this.log.Error("Unable to stop DeviceBootstrap logic", () => new { e });
            }
        }

        private void TryStopConnectLogic()
        {
            try
            {
                this.connectLogic.Stop();
            }
            catch (Exception e)
            {
                this.log.Error("Unable to stop Connect logic", () => new { e });
            }
        }
    }
}
