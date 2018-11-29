// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties
{
    /// <summary>
    /// Logic executed after Connect() succeeds, to send device properties updates.
    /// </summary>
    public class UpdateReportedProperties : IDevicePropertiesLogic
    {
        private readonly ILogger log;

        private string deviceId;

        private IDevicePropertiesActor context;

        public UpdateReportedProperties(ILogger logger)
        {
            this.log = logger;
        }

        public void Init(IDevicePropertiesActor context, string deviceId, IDevices devices)
        {
            this.context = context;
            this.deviceId = deviceId;
        }

        public async Task RunAsync()
        {
            this.log.Debug("Sending device properties update...", () => new { this.deviceId });

            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            try
            {
                var properties = this.context.DeviceProperties.GetAll();
                var state = this.context.DeviceState.GetAll();

                this.log.Debug("Checking to see if device is online", () => new { this.deviceId });
                if (!state.ContainsKey("online") || (bool) state["online"])
                {
                    // Device could be rebooting, updating firmware, etc.
                    this.log.Debug("The device state says the device is online", () => new { this.deviceId });

                    // Update the device twin with the current device properties state
                    await this.context.Client.UpdatePropertiesAsync(this.context.DeviceProperties);

                    var timeSpentMsecs = GetTimeSpentMsecs();
                    this.log.Debug("Device property updates delivered",
                        () => new { timeSpentMsecs, this.deviceId, properties });

                    // Mark properties as updated
                    this.context.DeviceProperties.ResetChanged();

                    this.context.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdated);
                }
                else
                {
                    // Device could be rebooting, updating firmware, etc.
                    var timeSpentMsecs = GetTimeSpentMsecs();
                    this.log.Debug("No properties will be updated because the device is offline",
                        () => new { timeSpentMsecs, this.deviceId });

                    this.context.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdateFailed);
                }
            }
            catch (BrokenDeviceClientException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Error while updating the reported properties in the device twin",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.context.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesClientBroken);
            }
            catch (PropertySendException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Failed to update device twin reported properties",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.context.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdateFailed);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Unexpected error while updating the reported properties in the device twin",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.context.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdateFailed);
            }
        }
    }
}
