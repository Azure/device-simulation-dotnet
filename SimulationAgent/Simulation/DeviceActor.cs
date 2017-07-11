// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation
{
    public interface IDeviceActor
    {
        IDeviceActor Setup(DeviceType deviceType, int position);

        void Start(CancellationToken cancellationToken);
    }

    public class DeviceActor : IDeviceActor
    {
        // When the actor fails to connect, it retries every 10 seconds
        private static readonly TimeSpan retryConnectingFrequency = TimeSpan.FromSeconds(10);

        // When connecting or sending a message, timeout after 5 seconds
        private static readonly TimeSpan connectionTimeout = TimeSpan.FromSeconds(5);

        // Used to make sure the actor checks at least every 10 seconds
        // if it has to stop
        private static readonly TimeSpan checkCancelationFrequency = TimeSpan.FromSeconds(10);

        // Possible states of the actor
        private enum Status
        {
            None = 0,
            Ready = 1,
            Connecting = 2,
            Connected = 3,
            Running = 4
        }

        private readonly ILogger log;
        private readonly IDevices devices;
        private readonly IScriptInterpreter scriptInterpreter;
        private readonly DependencyResolution.IFactory factory;

        /// <summary>
        /// The status of this device simulation actor, e.g. whether
        /// it's connecting, connected, sending, etc.
        /// </summary>
        private Status status;

        /// <summary>IoT Hub client</summary>
        private IDeviceClient client;

        /// <summary>
        /// All the information about the device type being simulated, like
        /// IoT Hub protocol, intervals, templates, supported methods, etc.
        /// </summary>
        private DeviceType deviceType;

        /// <summary>ID of the simulated device</summary>
        private string deviceId;

        /// <summary>
        /// A timer used for the action of the current state. It's used to
        /// retry connecting, and to keep refreshing the device state.
        /// </summary>
        private readonly ITimer timer;

        /// <summary>
        /// A collection of timers used to send messages. Each simulated device
        /// can send multiple messages, with different frequency.
        /// </summary>
        private List<ITimer> telemetryTimers;

        /// <summary>
        /// A timer used to check whether the simulation stopped and the actor
        /// should stop running.
        /// </summary>
        private readonly ITimer cancelationCheckTimer;

        /// <summary>
        /// A token used to signal the actor to stop when the simulation stops.
        /// </summary>
        private CancellationToken cancellationToken;

        /// <summary>The protocol used with Azure IoT Hub</summary>
        private IoTHubProtocol ioTHubProtocol;

        /// <summary>
        /// The simulated state of the simulated device. The state is
        /// periodically updated using an external script.
        /// </summary>
        private Dictionary<string, object> deviceState;

        /// <summary>
        /// How ofthen the simulated device state needs to be updated, i.e.
        /// when to execute the external script.
        /// </summary>
        private TimeSpan deviceStateInterval;

        public DeviceActor(
            ILogger logger,
            IDevices devices,
            IScriptInterpreter scriptInterpreter,
            DependencyResolution.IFactory factory)
        {
            this.log = logger;
            this.devices = devices;
            this.scriptInterpreter = scriptInterpreter;
            this.factory = factory;

            this.status = Status.None;
            this.timer = this.factory.Resolve<ITimer>();
            this.cancelationCheckTimer = this.factory.Resolve<ITimer>();
            this.telemetryTimers = new List<ITimer>();
        }

        /// <summary>
        /// Invoke this method before calling Start(), to initialize the actor
        /// with details like the device type and message type to simulate.
        /// If this method is not called before Start(), the application will
        /// thrown an exception.
        /// Setup() should be called only once, typically after the constructor.
        /// </summary>
        public IDeviceActor Setup(DeviceType deviceType, int position)
        {
            if (this.status != Status.None)
            {
                this.log.Error("The actor is already initialized",
                    () => new
                    {
                        CurrentDeviceId = this.deviceId,
                        NewDeviceType = deviceType.Name,
                        NewPosition = position
                    });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.deviceType = deviceType;
            this.deviceId = "Simulated." + deviceType.Name + "." + position;
            this.ioTHubProtocol = deviceType.Protocol;

            this.deviceStateInterval = deviceType.DeviceState.SimulationInterval;
            this.deviceState = CloneObject(deviceType.DeviceState.Initial);
            this.log.Debug("Initial device state", () => new { this.deviceId, this.deviceState });

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
            switch (this.status)
            {
                default:
                    this.log.Debug("The actor already started", () => new { this.deviceId });
                    break;

                case Status.None:
                    this.log.Error("The actor is not initialized", () => { });
                    throw new DeviceActorNotInitializedException();

                case Status.Ready:
                    this.cancellationToken = token;
                    this.log.Debug("Starting...", () => new { this.deviceId });
                    this.MoveNext();
                    break;
            }
        }

        /// <summary>
        /// An optional method to stop the device actor, instead of using the
        /// cancellation token.
        /// </summary>
        private void Stop()
        {
            this.StopTimers();
            this.client?.DisconnectAsync().Wait(connectionTimeout);
            this.status = Status.Ready;
            this.log.Debug("Stopped", () => new { this.deviceId });
        }

        /// <summary>
        /// Logic executed after Start(), to establish a connection to IoT Hub.
        /// If the connection fails, the actor retries automatically after some
        /// seconds.
        /// </summary>
        /// <param name="context">The actor instance (i.e. 'this')</param>
        private static void Connect(object context)
        {
            var self = (DeviceActor) context;
            if (self.cancellationToken.IsCancellationRequested)
            {
                self.Stop();
                return;
            }

            if (self.status == Status.Connecting)
            {
                self.log.Debug("Connecting...", () => { });

                try
                {
                    var task = self.devices.GetOrCreateAsync(self.deviceId);
                    task.Wait(connectionTimeout);
                    var device = task.Result;
                    self.log.Debug("Device credentials retrieved", () => new { self.deviceId });

                    self.client = self.devices.GetClient(device, self.ioTHubProtocol);
                    self.log.Debug("Connection successful", () => new { self.deviceId });

                    self.MoveNext();
                }
                catch (Exception e)
                {
                    self.log.Error("Connection failed",
                        () => new { self.deviceId, e.Message, Error = e.GetType().FullName });
                }
            }
        }

        /// <summary>
        /// Periodically update the device state (i.e. sensors data), executing
        /// the script provided in the device type configuration.
        /// </summary>
        /// <param name="context">The actor instance (i.e. 'this')</param>
        private static void UpdateDeviceState(object context)
        {
            var self = (DeviceActor) context;
            if (self.cancellationToken.IsCancellationRequested)
            {
                self.Stop();
                return;
            }

            var scriptContext = new Dictionary<string, object>
            {
                ["currentTime"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                ["deviceId"] = self.deviceId,
                ["deviceType"] = self.deviceType.Name
            };

            self.log.Debug("Updating device status", () => new { self.deviceId, self.deviceState });

            lock (self.deviceState)
            {
                self.deviceState = self.scriptInterpreter.Invoke(
                    self.deviceType.DeviceState.SimulationScript,
                    scriptContext,
                    self.deviceState);
            }

            self.log.Debug("New device status", () => new { self.deviceId, self.deviceState });

            // Start sending telemetry messages
            if (self.status == Status.Connected)
            {
                self.MoveNext();
            }
        }

        /// <summary>
        /// Logic executed after Connect() succeeds, to send telemetry.
        /// </summary>
        /// <param name="context">The message to send and a reference to the
        /// actor instance (i.e. 'this')</param>
        private static void SendTelemetry(object context)
        {
            var callContext = (TelemetryContext) context;
            var self = callContext.Self;
            var message = callContext.Message;

            if (self.cancellationToken.IsCancellationRequested)
            {
                self.Stop();
                return;
            }

            // Send the telemetry message
            try
            {
                // Inject the device state into the message template
                var msg = message.MessageTemplate;
                lock (self.deviceState)
                {
                    foreach (var value in self.deviceState)
                    {
                        msg = msg.Replace("${" + value.Key + "}", value.Value.ToString());
                    }
                }

                self.log.Debug("SendTelemetry...",
                    () => new { self.deviceId, MessageSchema = message.MessageSchema.Name, msg });
                self.client
                    .SendMessageAsync(msg, message.MessageSchema)
                    .Wait(connectionTimeout);
            }
            catch (Exception e)
            {
                self.log.Error("SendTelemetry failed",
                    () => new { self.deviceId, e.Message, Error = e.GetType().FullName });
            }

            self.log.Debug("SendTelemetry complete", () => new { self.deviceId });
        }

        /// <summary>
        /// When the telemetry is sent very not very often, for example once
        /// every 5 minutes, this method is executed more frequently, to check
        /// whether the user has asked to stop the simulation.
        /// </summary>
        /// <param name="context">The actor instance (i.e. 'this')</param>
        private static void CancelationCheck(object context)
        {
            var self = (DeviceActor) context;
            if (self.cancellationToken.IsCancellationRequested)
            {
                self.Stop();
            }
        }

        /// <summary>
        /// Check whether a second timer is required, to periodically check if
        /// the user asks to stop the simulation. This happens when the actor
        /// remains inactive for long periods, for example when sending
        /// telemetry every 5 minutes.
        /// </summary>
        /// <param name="curr"></param>
        private void ScheduleCancelationCheckIfRequired(TimeSpan curr)
        {
            if (curr > checkCancelationFrequency)
            {
                this.cancelationCheckTimer.Setup(
                    CancelationCheck, this, checkCancelationFrequency);
                this.cancelationCheckTimer.Start();
            }
        }

        private void StopTimers()
        {
            this.log.Debug("Stopping timers", () => { });
            this.timer.Stop();
            this.cancelationCheckTimer.Stop();

            foreach (var t in this.telemetryTimers) t.Stop();
            this.telemetryTimers = new List<ITimer>();
        }

        /// <summary>
        /// State machine flow. Change the internal state and schedule the
        /// execution of the new corresponding logic.
        /// </summary>
        private void MoveNext()
        {
            switch (this.status)
            {
                case Status.None:
                    this.log.Debug("Moving actor state to Ready", () => new { this.deviceId });
                    this.status = Status.Ready;
                    break;

                case Status.Ready:
                    this.log.Debug("Moving actor state to Connecting", () => new { this.deviceId });
                    this.status = Status.Connecting;
                    this.StopTimers();
                    this.timer.Setup(Connect, this, retryConnectingFrequency);
                    this.timer.Start();
                    this.ScheduleCancelationCheckIfRequired(retryConnectingFrequency);
                    break;

                case Status.Connecting:
                    this.log.Debug("Moving actor state to Connected", () => new { this.deviceId });
                    this.status = Status.Connected;
                    this.StopTimers();
                    this.timer.Setup(UpdateDeviceState, this, this.deviceStateInterval);
                    this.timer.Start();
                    this.ScheduleCancelationCheckIfRequired(this.deviceStateInterval);
                    break;

                case Status.Connected:
                    this.log.Debug("Moving actor state to Running", () => new { this.deviceId });
                    this.status = Status.Running;

                    foreach (var message in this.deviceType.Telemetry)
                    {
                        var telemetryTimer = this.factory.Resolve<ITimer>();
                        this.telemetryTimers.Add(telemetryTimer);
                        var callContext = new TelemetryContext
                        {
                            Self = this,
                            Message = message
                        };

                        this.log.Debug("Scheduling SendTelemetry", () =>
                            new { message.Interval.TotalSeconds, message.MessageSchema.Name, message.MessageTemplate });

                        telemetryTimer.Setup(SendTelemetry, callContext, message.Interval);
                        telemetryTimer.StartIn(message.Interval);
                    }
                    break;
            }
        }

        private static T CloneObject<T>(T source)
        {
            return JsonConvert.DeserializeObject<T>(
                JsonConvert.SerializeObject(source));
        }

        private class TelemetryContext
        {
            public DeviceActor Self { get; set; }
            public DeviceType.DeviceTypeMessage Message { get; set; }
        }
    }
}
