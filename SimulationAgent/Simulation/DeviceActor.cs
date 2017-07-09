// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation
{
    public interface IDeviceActor
    {
        IDeviceActor Setup(
            DeviceType deviceType,
            int position,
            DeviceType.DeviceTypeMessage messageTemplate);

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
            Connected = 3
        }

        private readonly ILogger log;
        private readonly IDevices devices;
        private readonly DependencyResolution.IFactory factory;
        private readonly IMessageGenerator messageGenerator;

        private IDeviceClient client;
        private DeviceType deviceType;
        private string deviceId;
        private Status status;
        private Device device;
        private DeviceType.DeviceTypeMessage msgTemplate;
        private string lastTelemetryMessage;
        private readonly ITimer timer;
        private readonly ITimer cancelationCheckTimer;
        private CancellationToken cancellationToken;

        public DeviceActor(
            ILogger logger,
            IDevices devices,
            DependencyResolution.IFactory factory,
            IMessageGenerator messageGenerator)
        {
            this.log = logger;
            this.devices = devices;
            this.factory = factory;
            this.messageGenerator = messageGenerator;
            this.status = Status.None;
            this.timer = this.factory.Resolve<ITimer>();
            this.cancelationCheckTimer = this.factory.Resolve<ITimer>();
        }

        /// <summary>
        /// Invoke this method before calling Start(), to initialize the actor
        /// with details like the device type and message type to simulate.
        /// If this method is not called before Start(), the application will
        /// thrown an exception.
        /// Setup() should be called only once, typically after the constructor.
        /// </summary>
        public IDeviceActor Setup(
            DeviceType deviceType,
            int position,
            DeviceType.DeviceTypeMessage messageTemplate)
        {
            if (this.status != Status.None)
            {
                this.log.Error("The actor is already initialized",
                    () => new
                    {
                        CurrentDeviceId = this.deviceId,
                        NewMessageSchema = messageTemplate.MessageSchema.Name,
                        NewDeviceType = deviceType.Name,
                        NewPosition = position
                    });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.deviceType = deviceType;
            this.deviceId = "Simulated." + deviceType.Name + "." + position;
            this.msgTemplate = messageTemplate;

            this.log.Debug("Setup complete",
                () => new
                {
                    this.deviceId,
                    MessageSchema = this.msgTemplate.MessageSchema.Name,
                    DeviceTypeName = deviceType.Name,
                    Position = position
                });

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
        public void Start(CancellationToken cancellationToken)
        {
            switch (this.status)
            {
                case Status.None:
                    this.log.Error("The actor is not initialized", () => { });
                    throw new DeviceActorNotInitializedException();

                case Status.Ready:
                    this.cancellationToken = cancellationToken;
                    this.log.Debug("Starting...", () => new { this.deviceId });
                    this.MoveNext();
                    break;

                default:
                    this.log.Debug("The actor already started",
                        () => new { this.deviceId });
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
            var actor = (DeviceActor) context;
            if (actor.cancellationToken.IsCancellationRequested)
            {
                actor.Stop();
                return;
            }

            if (actor.status == Status.Connecting)
            {
                actor.log.Debug("Connecting...", () => { });

                try
                {
                    var task = actor.devices.GetOrCreateAsync(actor.deviceId);
                    task.Wait(connectionTimeout);
                    actor.device = task.Result;
                    actor.log.Debug("Device credentials retrieved",
                        () => new { actor.deviceId });

                    actor.client = actor.devices.GetClient(
                        actor.device,
                        actor.deviceType.Protocol);
                    actor.log.Debug("Connection successful",
                        () => new { actor.deviceId });

                    actor.MoveNext();
                }
                catch (Exception e)
                {
                    actor.log.Error("Connection failed",
                        () => new { actor.deviceId, e.Message, Error = e.GetType().FullName });
                }
            }
        }

        /// <summary>
        /// Logic executed after Connect() succeeds, to send telemetry.
        /// </summary>
        /// <param name="context">The actor instance (i.e. 'this')</param>
        private static void SendTelemetry(object context)
        {
            var actor = (DeviceActor) context;
            if (actor.cancellationToken.IsCancellationRequested)
            {
                actor.Stop();
                return;
            }

            actor.lastTelemetryMessage = actor.messageGenerator.Generate(
                actor.deviceType,
                actor.msgTemplate.MessageTemplate,
                actor.lastTelemetryMessage,
                actor.deviceId);

            actor.log.Debug("SendTelemetry...",
                () => new
                {
                    actor.deviceId,
                    MessageSchema = actor.msgTemplate.MessageSchema.Name,
                    actor.lastTelemetryMessage
                });

            try
            {
                actor.client
                    .SendMessageAsync(
                        actor.lastTelemetryMessage,
                        actor.msgTemplate.MessageSchema)
                    .Wait(connectionTimeout);
            }
            catch (Exception e)
            {
                actor.log.Error("SendTelemetry failed",
                    () => new { actor.deviceId, e.Message, Error = e.GetType().FullName });
            }

            actor.log.Debug("SendTelemetry complete",
                () => new { actor.deviceId, MessageSchema = actor.msgTemplate.MessageSchema.Name });
        }

        /// <summary>
        /// When the telemetry is sent very not very often, for example once
        /// every 5 minutes, this method is executed more frequently, to check
        /// whether the user has asked to stop the simulation.
        /// </summary>
        /// <param name="context">The actor instance (i.e. 'this')</param>
        private static void CancelationCheck(object context)
        {
            var actor = (DeviceActor) context;
            if (actor.cancellationToken.IsCancellationRequested)
            {
                actor.Stop();
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
            this.timer.Stop();
            this.cancelationCheckTimer.Stop();
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
                    this.status = Status.Ready;
                    break;

                case Status.Ready:
                    this.status = Status.Connecting;
                    this.StopTimers();
                    this.timer.Setup(Connect, this, retryConnectingFrequency);
                    this.timer.Start();
                    this.ScheduleCancelationCheckIfRequired(retryConnectingFrequency);
                    break;

                case Status.Connecting:
                    this.status = Status.Connected;
                    this.StopTimers();
                    this.timer.Setup(SendTelemetry, this, this.msgTemplate.Interval);
                    this.timer.Start();
                    this.ScheduleCancelationCheckIfRequired(this.msgTemplate.Interval);
                    break;
            }
        }
    }
}
