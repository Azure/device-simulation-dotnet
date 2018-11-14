// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Devices
{
    public class DeviceApiModel
    {
        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";

        [JsonProperty(PropertyName = "ETag", NullValueHandling = NullValueHandling.Ignore)]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Enabled")]
        public bool? Enabled { get; set; }

        [JsonProperty(PropertyName = "Connected")]
        public bool? Connected { get; set; }

        [JsonProperty(PropertyName = "C2DMessageCount", NullValueHandling = NullValueHandling.Ignore)]
        public string C2DMessageCount { get; set; }

        [JsonProperty(PropertyName = "LastActivity", NullValueHandling = NullValueHandling.Ignore)]
        public string LastActivity { get; set; }

        [JsonProperty(PropertyName = "LastStatusUpdated", NullValueHandling = NullValueHandling.Ignore)]
        public string LastStatusUpdated { get; set; }

        [JsonProperty(PropertyName = "IoTHubHostName", NullValueHandling = NullValueHandling.Ignore)]
        public string IoTHubHostName { get; set; }

        [JsonProperty(PropertyName = "AuthPrimaryKey", NullValueHandling = NullValueHandling.Ignore)]
        public string AuthPrimaryKey { get; set; }

        public DeviceApiModel()
        {
            this.ETag = string.Empty;
            this.Id = string.Empty;

            // When unspecified, a device is enabled
            this.Enabled = true;

            // When unspecified, a device is connected
            this.Connected = true;

            this.C2DMessageCount = null;
            this.LastActivity = null;
            this.LastStatusUpdated = null;
            this.IoTHubHostName = null;
            this.AuthPrimaryKey = null;
        }

        public static DeviceApiModel FromServiceModel(Device value)
        {
            if (value == null) return null;

            var result = new DeviceApiModel
            {
                ETag = value.ETag,
                Id = value.Id,
                Enabled = value.Enabled,
                Connected = value.Connected,
                IoTHubHostName = value.IoTHubHostName,
                AuthPrimaryKey = value.AuthPrimaryKey
            };

            if (value.LastActivity.HasValue)
            {
                result.LastActivity = value.LastActivity?.ToString(DATE_FORMAT);
            }

            if (value.LastStatusUpdated.HasValue)
            {
                result.LastStatusUpdated = value.LastStatusUpdated?.ToString(DATE_FORMAT);
            }

            return result;
        }
    }
}
