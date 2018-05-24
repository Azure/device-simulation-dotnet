// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

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
        public static DeviceModelTelemetryMessageSchema FromServiceModel(DeviceModelMessageSchema value)
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

        // Map API model to service model
        public static DeviceModelMessageSchema ToServiceModel(DeviceModelTelemetryMessageSchema value)
        {
            if (value == null) return null;

            Enum.TryParse(value.Format, out DeviceModelMessageSchemaFormat format);
            var result = new DeviceModelMessageSchema
            {
                Name = value.Name,
                Format = format
            };

            foreach (var field in value.Fields)
            {
                Enum.TryParse(field.Value, out DeviceModelMessageSchemaType fieldValue);
                result.Fields.Add(field.Key, fieldValue);
            }

            return result;
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(this.Name)
                   || string.IsNullOrEmpty(this.Format)
                   || (this.Fields == null || this.Fields.Count == 0);
        }
    }
}
