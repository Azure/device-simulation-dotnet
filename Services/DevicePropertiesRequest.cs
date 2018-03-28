// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDevicePropertiesRequest
    {
        Task RegisterDevicePropertyUpdatesAsync(
            string deviceId,
            ISmartDictionary deviceProperties);

        Task OnPropertyUpdateRequestedCallback(
            TwinCollection desiredProperties,
            object userContext);
    }

    public class DevicePropertiesRequest : IDevicePropertiesRequest
    {
        private readonly Azure.Devices.Client.DeviceClient client;
        private readonly ILogger log;
        private string deviceId;
        private ISmartDictionary deviceProperties;

        public DevicePropertiesRequest(Azure.Devices.Client.DeviceClient client, ILogger logger)
        {
            this.client = client;
            this.log = logger;
            this.deviceId = string.Empty;
        }

        public async Task RegisterDevicePropertyUpdatesAsync(string deviceId, ISmartDictionary deviceProperties)
        {
            if (this.deviceId != string.Empty)
            {
                this.log.Error("Application error, each device must have a separate instance", () => { });
                throw new Exception("Application error, each device must have a separate instance of " + this.GetType().FullName);
            }

            this.deviceId = deviceId;
            this.deviceProperties = deviceProperties;

            this.log.Debug("Setting up callback for desired properties updates.", () => new { this.deviceId });

            // Set callback that IoT Hub calls whenever the client receives a state update (desired or reported).
            // This has the side-effect of subscribing to the PATCH topic on the service.
            // https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.deviceclient.setdesiredpropertyupdatecallbackasync
            await this.client.SetDesiredPropertyUpdateCallbackAsync(OnPropertyUpdateRequestedCallback, null);

            this.log.Debug("Callback for desired properties updates setup successfully", () => new { this.deviceId });
        }

        /// <summary>
        /// When a desired property change is requested, update the internal the device state properties
        /// which will be reported to the hub. If there is a new desired property that does not exist in
        /// the reported properties, it will be added.
        /// </summary>
        public Task OnPropertyUpdateRequestedCallback(TwinCollection desiredProperties, object userContext)
        {
            this.log.Info("Desired property update requested", () => new { this.deviceId, desiredProperties });

            if (desiredProperties != null && desiredProperties.Count > 0)
            {
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
                    this.log.Error("Error updating internal device state properties", () => new { e, this.deviceId, desiredProperties });
                }

                this.log.Debug("Desired property update successfully reported to internal state", () => new { this.deviceId, desiredProperties });
            }

            return Task.CompletedTask;
        }
    }
}
