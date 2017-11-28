// Copyright (c) Microsoft. All rights reserved.

using System;
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

        public void Run()
        {
            this.log.Debug("Adding tag to device twin...", () => new { this.deviceId });

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.devices.AddTagAsync(this.deviceId)
                .ContinueWith(t =>
                {
                    var timeTaken = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                    this.log.Debug("Device tag added", () => new { this.deviceId, timeTaken });
                    this.context.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceTwinTagged);
                });
        }
    }
}
