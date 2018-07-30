// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
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
        private IDevicePropertiesActor deviceContext;
        private ISimulationContext simulationContext;
        private string deviceId;

        public SetDeviceTag(ILogger logger, IInstance instance)
        {
            this.log = logger;
            this.instance = instance;
        }

        public void Init(IDevicePropertiesActor deviceContext, string deviceId)
        {
            this.instance.InitOnce();
            this.deviceContext = deviceContext;
            this.simulationContext = deviceContext.SimulationContext;
            this.deviceId = deviceId;
            this.instance.InitComplete();
        }

        public async Task RunAsync()
        {
            this.instance.InitRequired();

            this.log.Debug("Adding tag to device twin...", () => new { this.deviceId });
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                await this.simulationContext.Devices.AddTagAsync(this.deviceId);
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Debug("Device tag set", () => new { timeSpentMsecs, this.deviceId });
                this.deviceContext.HandleEvent(DevicePropertiesActor.ActorEvents.DeviceTagged);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Error while tagging the device twin", () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceContext.HandleEvent(DevicePropertiesActor.ActorEvents.DeviceTaggingFailed);
            }
        }
    }
}
