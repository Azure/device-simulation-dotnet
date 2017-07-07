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

        Task ConnectAsync();
        void Start();
        void Stop();
    }

    public class DeviceActor : IDeviceActor
    {
        private readonly ILogger log;
        private readonly IDevices devices;
        private readonly DependencyResolution.IFactory factory;

        private DeviceType deviceType;
        private string deviceId;
        private DeviceType.DeviceTypeMessage message;

        private ITimer timer;
        private Device device;
        private bool isConnected;
        private bool isStarted;

        public DeviceActor(
            ILogger logger,
            IDevices devices,
            DependencyResolution.IFactory factory)
        {
            this.log = logger;
            this.devices = devices;
            this.factory = factory;

            this.isConnected = false;
            this.isStarted = false;
        }

        public IDeviceActor Setup(
            DeviceType deviceType,
            int position,
            DeviceType.DeviceTypeMessage message)
        {
            this.deviceType = deviceType;
            this.deviceId = "Simulated." + deviceType.Name + "." + position;
            this.message = message;

            return this;
        }

        public async Task ConnectAsync()
        {
            if (this.isConnected) return;
            if (string.IsNullOrEmpty(this.deviceId))
            {
                this.log.Error("The actor is not initialized",
                    () => new { this.deviceId });
                throw new DeviceActorNotInitializedException();
            }

            this.log.Debug("Connect...", () => { });

            this.device = await this.devices.GetOrCreateAsync(this.deviceId);
            this.isConnected = true;

            this.log.Debug("Connect complete", () => { });
        }

        public void Start()
        {
            if (this.isStarted) return;
            if (this.deviceType == null || this.message == null)
            {
                this.log.Error("The actor is not initialized", () => { });
                throw new DeviceActorNotInitializedException();
            }

            this.log.Debug("Start...",
                () => new { this.deviceId, MessageSchema = this.message.MessageSchema.Name });

            if (!this.isConnected)
            {
                this.log.Error("The actor is not ready, wait for ConnectAsync to complete",
                    () => new { this.deviceId });
                throw new Exception("The actor is not ready, wait for ConnectAsync to complete");
            }

            this.timer = this.factory.Resolve<ITimer>();
            this.timer.Setup(SendTelemetry, this, (int)this.message.Interval.TotalMilliseconds);
            this.timer.Start();

            this.isStarted = true;

            this.log.Debug("Start complete",
                () => new { this.deviceId, MessageSchema = this.message.MessageSchema.Name });
        }

        public void Stop()
        {
            if (!this.isStarted) return;

            this.log.Debug("Stop...",
                () => new { this.deviceId, MessageSchema = this.message.MessageSchema.Name });

            this.timer.Stop();

            this.isStarted = false;

            this.log.Debug("Stop complete",
                () => new { this.deviceId, MessageSchema = this.message.MessageSchema.Name });
        }

        private static void SendTelemetry(object context)
        {
            var actor = (DeviceActor) context;

            actor.log.Debug("SendTelemetry...",
                () => new { actor.deviceId, MessageSchema = actor.message.MessageSchema.Name });

            if (!actor.isConnected) return;
            if (!actor.isStarted) return;

            var client = actor.devices.GetClient(actor.device, actor.deviceType.Protocol);
            client.SendMessageAsync(actor.message).Wait();

            actor.log.Debug("SendTelemetry complete",
                () => new { actor.deviceId, MessageSchema = actor.message.MessageSchema.Name });
        }
    }
}
