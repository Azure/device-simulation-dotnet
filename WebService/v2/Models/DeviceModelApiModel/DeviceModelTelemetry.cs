// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Models.DeviceModelApiModel
{
    public class DeviceModelTelemetry
    {
        [JsonProperty(PropertyName = "Interval")]
        public string Interval { get; set; }

        [JsonProperty(PropertyName = "MessageTemplate")]
        public string MessageTemplate { get; set; }

        [JsonProperty(PropertyName = "MessageSchema")]
        public DeviceModelTelemetryMessageSchema MessageSchema { get; set; }

        public DeviceModelTelemetry()
        {
            this.Interval = "00:00:00";
            this.MessageTemplate = string.Empty;
            this.MessageSchema = new DeviceModelTelemetryMessageSchema();
        }

        // Map service model to API model
        public static DeviceModelTelemetry FromServiceModel(DeviceModelMessage value)
        {
            if (value == null) return null;

            var result = new DeviceModelTelemetry
            {
                Interval = value.Interval.ToString("c"),
                MessageTemplate = value.MessageTemplate,
                MessageSchema = DeviceModelTelemetryMessageSchema.FromServiceModel(value.MessageSchema)
            };

            return result;
        }

        // Map API model to service model
        public static DeviceModelMessage ToServiceModel(DeviceModelTelemetry value)
        {
            if (value == null) return null;

            var result = new DeviceModelMessage
            {
                Interval = TimeSpan.Parse(value.Interval),
                MessageTemplate = value.MessageTemplate,
                MessageSchema = DeviceModelTelemetryMessageSchema.ToServiceModel(value.MessageSchema)
            };

            return result;
        }

        public void ValidateInputRequest(ILogger log)
        {
            const string NO_ETAG = "The custom device model doesn't contain a ETag";

            // A message must contain a validate interval
            try
            {

            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
