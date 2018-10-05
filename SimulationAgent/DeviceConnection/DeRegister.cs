// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Deregister the device from the hub registry
    /// </summary>
    public class Deregister : IDeviceConnectionLogic
    {
        private readonly ILogger log;
        private readonly IInstance instance;
        private string deviceId;
        private IDeviceConnectionActor deviceConnectionActor;

        public Deregister(ILogger logger, IInstance instance)
        {
            this.log = logger;
            this.instance = instance;
        }

        public void Init(IDeviceConnectionActor actor, string deviceId, DeviceModel deviceModel)
        {
            this.instance.InitOnce();

            this.deviceConnectionActor = actor;
            this.deviceId = deviceId;

            this.instance.InitComplete();
        }

        public async Task RunAsync()
        {
            this.instance.InitRequired();

            this.log.Debug("Deregistering device...", () => new { this.deviceId });

            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await this.deviceConnectionActor.SimulationContext.Devices.DeleteAsync(this.deviceId);

                var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                this.log.Debug("Device deregistered", () => new { this.deviceId, timeSpent });

                this.deviceConnectionActor.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceDeregistered);
            }
            catch (Exception e)
            {
                this.log.Error("Error while registering the device", () => new { this.deviceId, e });
                this.deviceConnectionActor.HandleEvent(DeviceConnectionActor.ActorEvents.DeregisterationFailed);
            }
        }
    }
}
