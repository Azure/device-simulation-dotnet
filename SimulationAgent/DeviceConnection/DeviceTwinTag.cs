// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Add twin information to the new device twin
    /// </summary>
    public class DeviceTwinTag : IDeviceConnectionLogic
    {
        private readonly IDevices devices;
        private readonly ILogger log;
        private string deviceId;
        private IDeviceConnectionActor context;

        public DeviceTwinTag(IDevices devices, ILogger logger)
        {
            this.log = logger;
            this.devices = devices;
        }

        public void Setup(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel)
        {
            this.context = context;
            this.deviceId = deviceId;
        }

        public async Task RunAsync()
        {
            this.log.Debug("Adding tag to device twin...", () => new { this.deviceId });
            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await this.devices.AddTagAsync(this.deviceId);

                var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                this.log.Debug("Device tag added", () => new { this.deviceId, timeSpent });

                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceTwinTagged);
            }
            catch (Exception e)
            {
                this.log.Error("Error while tagging the device twin", () => new { this.deviceId, e });
                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceTwinTaggingFailed);
            }
        }
    }
}
