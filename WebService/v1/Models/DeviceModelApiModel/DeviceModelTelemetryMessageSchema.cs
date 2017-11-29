// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel
{
    public class DeviceModelTelemetryMessageSchema
    {
        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "Format")]
        public string Format { get; set; }

        [JsonProperty(PropertyName = "Fields")]
        public IDictionary<string, string> Fields { get; set; }

        public DeviceModelTelemetryMessageSchema()
        {
            this.Name = string.Empty;
            this.Format = "JSON";
            this.Fields = new Dictionary<string, string>();
        }

        // Map service model to API model
        public static DeviceModelTelemetryMessageSchema FromServiceModel(DeviceModel.DeviceModelMessageSchema value)
        {
            if (value == null) return null;

            var result = new DeviceModelTelemetryMessageSchema
            {
                Name = value.Name,
                Format = value.Format.ToString()
            };

            foreach (var field in value.Fields)
            {
                result.Fields.Add(field.Key, field.Value.ToString());
            }

            return result;
        }
    }
}
