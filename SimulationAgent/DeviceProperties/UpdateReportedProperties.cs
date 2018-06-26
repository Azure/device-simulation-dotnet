// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

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

        public void Setup(IDevicePropertiesActor context, string deviceId)
        {
            this.context = context;
            this.deviceId = deviceId;
        }

        public void Run()
        {
            this.log.Debug("Sending device properties update...", () => new { this.deviceId });
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                var properties = this.context.DeviceProperties.GetAll();
                var state = this.context.DeviceState.GetAll();

                this.log.Debug("Checking to see if device is online", () => new { this.deviceId });
                if ((bool) state["online"])
                {
                    // Device could be rebooting, updating firmware, etc.
                    this.log.Debug("The device state says the device is online", () => new { this.deviceId });

                    // Update the device twin with the current device properites state
                    this.context.Client.UpdatePropertiesAsync(this.context.DeviceProperties)
                        .ContinueWith(t =>
                        {
                            var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                            this.log.Debug("Device property updates delivered", () => new { timeSpentMsecs, this.deviceId, properties });
                            this.context.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdated);
                        });

                    // Mark properties as updated
                    this.context.DeviceProperties.ResetChanged();
                }
                else
                {
                    // Device could be rebooting, updating firmware, etc.
                    var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                    this.log.Debug("No properties will be updated as the device is offline...", () => new { timeSpentMsecs, this.deviceId });
                    this.context.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdateFailed);
                }
            }
            catch (Exception e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Device properties error", () => new { timeSpentMsecs, this.deviceId, e });
                this.context.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdateFailed);
            }
        }
    }
}
