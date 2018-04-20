// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel
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
        public static DeviceModelTelemetry FromServiceModel(DeviceModel.DeviceModelMessage value)
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
    }
}
