// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDevicePropertiesRequest
    {
        Task RegisterChangeUpdateAsync(
            string deviceId,
            ISmartDictionary deviceProperties);
    }

    public class DeviceProperties : IDevicePropertiesRequest
    {
        private readonly IDeviceClientWrapper client;
        private readonly ILogger log;
        private string deviceId;
        private ISmartDictionary deviceProperties;
        private bool isRegistered;

        public DeviceProperties(IDeviceClientWrapper client, ILogger logger)
        {
            this.client = client;
            this.log = logger;
            this.deviceId = string.Empty;
            this.isRegistered = false;
        }

        public async Task RegisterChangeUpdateAsync(string deviceId, ISmartDictionary deviceProperties)
        {
            if (this.isRegistered)
            {
                this.log.Error("Application error, each device must have a separate instance", () => { });
                throw new Exception("Application error, each device must have a separate instance of " + this.GetType().FullName);
            }

            this.deviceId = deviceId;
            this.deviceProperties = deviceProperties ?? new SmartDictionary();

            this.log.Debug("Setting up callback for desired properties updates.", () => new { this.deviceId });

            // Set callback that IoT Hub calls whenever the client receives a desired properties state update.
            await this.client.SetDesiredPropertyUpdateCallbackAsync(this.OnChangeCallback, null);

            this.log.Debug("Callback for desired properties updates setup successfully", () => new { this.deviceId });

            this.isRegistered = true;
        }

        /// <summary>
        /// When a desired property change is requested, update the internal device state properties
        /// which will be reported to the hub. If there is a new desired property that does not exist in
        /// the reported properties, it will be added.
        /// </summary>
        private Task OnChangeCallback(TwinCollection desiredProperties, object userContext)
        {
            this.log.Debug("Desired property update requested", () => new { this.deviceId, desiredProperties });

            // This is where custom code for handling specific desired property changes could be added.
            // For the purposes of the simulation service, we have chosen to write the desired properties
            // directly to the reported properties. 
            try
            {
                foreach (KeyValuePair<string, object> item in desiredProperties)
                {
                    // Only update if key doesn't exist or value has changed 
                    if (!this.deviceProperties.Has(item.Key) ||
                        (item.Value.ToString() != this.deviceProperties.Get(item.Key).ToString()))
                    {
                        // Update existing property or create new property if key doesn't exist.
                        this.deviceProperties.Set(item.Key, item.Value);
                    }
                }
            }
            catch (Exception e)
            {
                this.log.Error("Error updating device properties to desired values", () => new { e, this.deviceId, desiredProperties });
            }

            this.log.Debug("Device properties updated to desired values", () => new { this.deviceId, desiredProperties });

            return Task.CompletedTask;
        }
    }
}
