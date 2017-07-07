// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
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

        void Start();
        void Stop();
    }

    public class DeviceActor : IDeviceActor
    {
        private enum State
        {
            None = 0,
            Ready = 1,
            Connecting = 2,
            Connected = 3,
            Sending = 4
        }

        private readonly ILogger log;
        private readonly IDevices devices;
        private readonly DependencyResolution.IFactory factory;

        private DeviceType deviceType;
        private string deviceId;
        private DeviceType.DeviceTypeMessage message;

        private State state;
        private readonly ITimer timer;
        private Device device;

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
        }

        public IDeviceActor Setup(
            DeviceType deviceType,
            int position,
            DeviceType.DeviceTypeMessage message)
        {
            if (this.state != State.None)
            {
                this.log.Error("The actor is already initialized", () => { });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.deviceType = deviceType;
            this.deviceId = "Simulated." + deviceType.Name + "." + position;
            this.message = message;
            this.MoveNext();

            return this;
        }

        public void Start()
        {
            if (this.state == State.None)
            {
                this.log.Error("The actor is not initialized", () => { });
                throw new DeviceActorNotInitializedException();
            }

            if (this.state == State.Ready)
            {
                this.log.Debug("Start...",
                    () => new { this.deviceId, MessageSchema = this.message.MessageSchema.Name });

                this.MoveNext();
            }
        }

        public void Stop()
        {
            this.log.Debug("Stop...",
                () => new { this.deviceId, MessageSchema = this.message.MessageSchema.Name });

            this.timer.Stop();
            this.state = State.Ready;

            this.log.Debug("Stop complete",
                () => new { this.deviceId, MessageSchema = this.message.MessageSchema.Name });
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
                        this.timer.Stop();
                        // Retry connecting every 10 seconds
                        this.timer.Setup(Connect, this, 10 * 1000);
                        this.timer.Start();
                        break;

                    case State.Connecting:
                        this.state = State.Connected;
                        this.timer.Stop();
                        this.timer.Setup(SendTelemetry, this, (int) this.message.Interval.TotalMilliseconds);
                        this.timer.Start();
                        break;

                    case State.Sending:
                        break;
                }
            }
        }

        private static void Connect(object context)
        {
            var actor = (DeviceActor) context;

            if (actor.state == State.Connecting)
            {
                actor.log.Debug("Connecting...", () => { });

                try
                {
                    var task = actor.devices.GetOrCreateAsync(actor.deviceId);
                    task.Wait(TimeSpan.FromSeconds(5));
                    actor.device = task.Result;
                    actor.log.Debug("Connection complete", () => { });
                    actor.MoveNext();
                }
                catch (Exception e)
                {
                    actor.log.Error("Connection failed", () => new { actor.deviceId, e.Message, Error = e.GetType().FullName });
                }
            }
        }

        private static void SendTelemetry(object context)
        {
            var actor = (DeviceActor) context;
            actor.state = State.Sending;

            actor.log.Debug("SendTelemetry...",
                () => new { actor.deviceId, MessageSchema = actor.message.MessageSchema.Name });

            var client = actor.devices.GetClient(actor.device, actor.deviceType.Protocol);

            try
            {
                client.SendMessageAsync(actor.message).Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception e)
            {
                actor.log.Error("Send failed", () => new { actor.deviceId, e.Message, Error = e.GetType().FullName });
            }

            actor.log.Debug("SendTelemetry complete",
                () => new { actor.deviceId, MessageSchema = actor.message.MessageSchema.Name });
        }
    }
}
