// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties
{
    /// <summary>
    /// Logic executed after Connect() succeeds, to send device properties updates.
    /// </summary>
    public class UpdateReportedProperties : IDevicePropertiesLogic
    {
        private readonly ILogger log;
        private readonly IInstance instance;
        private IDevicePropertiesActor deviceContext;
        private ISimulationContext simulationContext;
        private string deviceId;

        public UpdateReportedProperties(ILogger logger, IInstance instance)
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

            this.log.Debug("Sending device properties update...", () => new { this.deviceId });
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                var properties = this.deviceContext.DeviceProperties.GetAll();
                var state = this.deviceContext.DeviceState.GetAll();

                this.log.Debug("Checking to see if device is online", () => new { this.deviceId });
                if ((bool) state["online"])
                {
                    // Device could be rebooting, updating firmware, etc.
                    this.log.Debug("The device state says the device is online", () => new { this.deviceId });

                    // Update the device twin with the current device properites state
                    await this.deviceContext.Client.UpdatePropertiesAsync(this.deviceContext.DeviceProperties);
                    var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                    this.log.Debug("Device property updates delivered", () => new { timeSpentMsecs, this.deviceId, properties });
                    this.deviceContext.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdated);

                    // Mark properties as updated
                    this.deviceContext.DeviceProperties.ResetChanged();
                }
                else
                {
                    // Device could be rebooting, updating firmware, etc.
                    var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                    this.log.Debug("No properties will be updated because the device is offline...", () => new { timeSpentMsecs, this.deviceId });
                    this.deviceContext.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdateFailed);
                }
            }
            catch (Exception e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Error while updating the reported properties in the device twin", () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceContext.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdateFailed);
            }
        }
    }
}
