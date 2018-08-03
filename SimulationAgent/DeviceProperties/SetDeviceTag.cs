// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
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

        public async Task RunAsync()
        {
            this.log.Debug("Adding tag to device twin...", () => new { this.deviceId });
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                await this.devices.AddTagAsync(this.deviceId);
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Debug("Device tag set", () => new { timeSpentMsecs, this.deviceId });
                this.context.HandleEvent(DevicePropertiesActor.ActorEvents.DeviceTagged);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Error while tagging the device twin", () => new { timeSpentMsecs, this.deviceId, e });
                this.context.HandleEvent(DevicePropertiesActor.ActorEvents.DeviceTaggingFailed);
            }
        }
    }
}
