// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties
{
    /// <summary>
    /// Add twin information to the new device twin
    /// </summary>
    public class SetDeviceTag : IDevicePropertiesLogic
    {
        private readonly IDevices devices;
        private readonly ILogger log;
        private string deviceId;
        private IDevicePropertiesActor context;

        public SetDeviceTag(IDevices devices, ILogger logger)
        {
            this.log = logger;
            this.devices = devices;
        }

        public void Setup(IDevicePropertiesActor context, string deviceId)
        {
            this.context = context;
            this.deviceId = deviceId;
        }

        public void Run()
        {
            this.log.Debug("Adding tag to device twin...", () => new { this.deviceId });
            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                this.devices.AddTagAsync(this.deviceId);

                var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                this.log.Debug("Device tag added", () => new { this.deviceId, timeSpent });

                this.context.HandleEvent(DevicePropertiesActor.ActorEvents.DeviceTagged);
            }
            catch (Exception e)
            {
                this.log.Error("Error while tagging the device twin", () => new { this.deviceId, e });
                this.context.HandleEvent(DevicePropertiesActor.ActorEvents.DeviceTaggingFailed);
            }
        }
    }
}
