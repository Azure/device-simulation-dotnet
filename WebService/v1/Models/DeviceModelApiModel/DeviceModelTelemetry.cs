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

        /// <summary>Map a service model to the corresponding API model</summary>
        public DeviceModelTelemetry(DeviceModel.DeviceModelMessage message) : this()
        {
            if (message == null) return;

            this.Interval = message.Interval.ToString("c");
            this.MessageTemplate = message.MessageTemplate;
            this.MessageSchema = new DeviceModelTelemetryMessageSchema(message.MessageSchema);
        }
    }
}