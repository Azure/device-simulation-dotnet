// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

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

            try
            {
                if (this.context.DeviceProperties.Changed)
                {
                    // There are no new device properties changes to push
                    this.log.Debug("No device properties to update...", () => new { this.deviceId });
                    this.context.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdated);
                    return;
                }
      
                var properties = this.context.DeviceProperties.GetAll();
                var state = this.context.DeviceState.GetAll();

                this.log.Debug("Checking to see if device is online", () => new { this.deviceId });
                if ((bool)state["online"])
                {
                    // device could be rebooting, updating firmware, etc.
                    this.log.Debug("The device state says the device is online", () => new { this.deviceId });

                    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    this.context.Client.UpdateTwinAsync()
                        .ContinueWith(t =>
                        {
                            var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                            this.log.Debug("Device property updates delivered", () => new { this.deviceId, timeSpent, Properties = properties});
                            this.context.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdated);
                        });
                }
                else
                {
                    // device could be rebooting, updating firmware, etc.
                    this.log.Debug("No properties will be updated as the device is offline...", () => new { this.deviceId });
                    this.context.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdateFailed);
                }
            } catch (Exception e)
            {
                this.log.Error("Device properties error", () => new { this.deviceId, e });
                this.context.HandleEvent(DevicePropertiesActor.ActorEvents.PropertiesUpdateFailed);
            }
        }
    }
}
