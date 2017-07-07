// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation
{
    public interface IDeviceActor
    {
        IDeviceActor Setup(
            DeviceType deviceType,
            int position,
            DeviceType.DeviceTypeMessage message);

        void Start(CancellationToken cancellationToken);
        void Stop();
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
        private enum State
        {
            None = 0,
            Ready = 1,
            Connecting = 2,
            Connected = 3
        }

        private readonly ILogger log;
        private readonly IDevices devices;
        private readonly DependencyResolution.IFactory factory;

        private DeviceType deviceType;
        private string deviceId;
        private DeviceType.DeviceTypeMessage message;

        private State state;
        private Device device;
        private readonly ITimer timer;
        private readonly ITimer cancelationCheckTimer;
        private CancellationToken cancellationToken;

        public DeviceActor(
            ILogger logger,
            IDevices devices,
            DependencyResolution.IFactory factory)
        {
            this.log = logger;
            this.devices = devices;
            this.factory = factory;

            this.state = State.None;

            this.timer = this.factory.Resolve<ITimer>();
            this.cancelationCheckTimer = this.factory.Resolve<ITimer>();
        }

        public IDeviceActor Setup(
            DeviceType deviceType,
            int position,
            DeviceType.DeviceTypeMessage message)
        {
            if (this.state != State.None)
            {
                this.log.Error("The actor is already initialized",
                    () => new
                    {
                        CurrentDeviceId = this.deviceId,
                        NewMessageSchema = message.MessageSchema.Name,
                        NewDeviceType = deviceType.Name,
                        NewPosition = position
                    });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.deviceType = deviceType;
            this.deviceId = "Simulated." + deviceType.Name + "." + position;
            this.message = message;

            this.log.Debug("Setup complete",
                () => new
                {
                    this.deviceId,
                    MessageSchema = this.message.MessageSchema.Name,
                    DeviceTypeName = deviceType.Name,
                    Position = position
                });

            this.MoveNext();

            return this;
        }

        public void Start(CancellationToken cancellationToken)
        {
            switch (this.state)
            {
                case State.None:
                    this.log.Error("The actor is not initialized", () => { });
                    throw new DeviceActorNotInitializedException();

                case State.Ready:
                    this.cancellationToken = cancellationToken;
                    this.log.Debug("Starting...", () => new { this.deviceId });
                    this.MoveNext();
                    break;

                default:
                    this.log.Debug("The actor already started", () => new { this.deviceId });
                    break;
            }
        }

        public void Stop()
        {
            lock (this.timer)
            {
                this.StopTimers();
                this.state = State.Ready;
                this.log.Debug("Stopped", () => new { this.deviceId });
            }
        }

        private static void Connect(object context)
        {
            var actor = (DeviceActor) context;
            if (actor.cancellationToken.IsCancellationRequested)
            {
                actor.Stop();
                return;
            }

            lock (actor.timer)
            {
                if (actor.state == State.Connecting)
                {
                    actor.log.Debug("Connecting...", () => { });

                    try
                    {
                        var task = actor.devices.GetOrCreateAsync(actor.deviceId);
                        task.Wait(connectionTimeout);
                        actor.device = task.Result;
                        actor.log.Debug("Connection successful", () => new { actor.deviceId });
                        actor.MoveNext();
                    }
                    catch (Exception e)
                    {
                        actor.log.Error("Connection failed", () => new { actor.deviceId, e.Message, Error = e.GetType().FullName });
                    }
                }
            }
        }

        private static void SendTelemetry(object context)
        {
            var actor = (DeviceActor) context;
            if (actor.cancellationToken.IsCancellationRequested)
            {
                actor.Stop();
                return;
            }

            lock (actor.timer)
            {
                actor.log.Debug("SendTelemetry...",
                    () => new { actor.deviceId, MessageSchema = actor.message.MessageSchema.Name });

                var client = actor.devices.GetClient(actor.device, actor.deviceType.Protocol);

                try
                {
                    client.SendMessageAsync(actor.message).Wait(connectionTimeout);
                }
                catch (Exception e)
                {
                    actor.log.Error("Send failed", () => new { actor.deviceId, e.Message, Error = e.GetType().FullName });
                }

                actor.log.Debug("SendTelemetry complete",
                    () => new { actor.deviceId, MessageSchema = actor.message.MessageSchema.Name });
            }
        }

        private static void CancelationCheck(object context)
        {
            var actor = (DeviceActor) context;
            if (actor.cancellationToken.IsCancellationRequested)
            {
                actor.Stop();
            }
        }

        private void ScheduleCancelationCheckIfRequired(TimeSpan curr)
        {
            if (curr > checkCancelationFrequency)
            {
                this.cancelationCheckTimer.Setup(CancelationCheck, this, checkCancelationFrequency);
                this.cancelationCheckTimer.Start();
            }
        }

        private void StopTimers()
        {
            this.timer.Stop();
            this.cancelationCheckTimer.Stop();
        }

        private void MoveNext()
        {
            lock (this.timer)
            {
                switch (this.state)
                {
                    case State.None:
                        this.state = State.Ready;
                        break;

                    case State.Ready:
                        this.state = State.Connecting;
                        this.StopTimers();
                        this.timer.Setup(Connect, this, retryConnectingFrequency);
                        this.timer.Start();
                        this.ScheduleCancelationCheckIfRequired(retryConnectingFrequency);
                        break;

                    case State.Connecting:
                        this.state = State.Connected;
                        this.StopTimers();
                        this.timer.Setup(SendTelemetry, this, this.message.Interval);
                        this.timer.Start();
                        this.ScheduleCancelationCheckIfRequired(this.message.Interval);
                        break;
                }
            }
        }
    }
}
