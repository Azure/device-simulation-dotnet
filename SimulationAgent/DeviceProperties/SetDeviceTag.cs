// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties
{
    /// <summary>
    /// Add twin information to the new device twin
    /// </summary>
    public class SetDeviceTag : IDevicePropertiesLogic
    {
        private readonly ILogger log;
        private readonly IInstance instance;

        private string deviceId;
        private IDevices devices;
        private IDevicePropertiesActor context;

        public SetDeviceTag(ILogger logger, IInstance instance)
        {
            this.log = logger;
            this.instance = instance;
        }

        public void Init(IDevicePropertiesActor context, string deviceId, IDevices devices)
        {
            this.instance.InitOnce();

            this.context = context;
            this.deviceId = deviceId;
            this.devices = devices;

            this.instance.InitComplete();
        }

        public async Task RunAsync()
        {
            this.log.Debug("Adding tag to device twin...", () => new { this.deviceId });

            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            try
            {
                await this.devices.AddTagAsync(this.deviceId);

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Debug("Device tag set", () => new { timeSpentMsecs, this.deviceId });

                this.context.HandleEvent(DevicePropertiesActor.ActorEvents.DeviceTagged);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Error while tagging the device twin",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.context.HandleEvent(DevicePropertiesActor.ActorEvents.DeviceTaggingFailed);
            }
        }
    }
}
