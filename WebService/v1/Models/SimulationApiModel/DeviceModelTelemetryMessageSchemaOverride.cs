// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    // SEE: <DeviceModelApiModel.DeviceModelTelemetryMessageSchema> for the original fields being overridden
    // Avoid subclassing <DeviceModelTelemetryMessageSchema> to exclude unused fields and different default values
    public class DeviceModelTelemetryMessageSchemaOverride
    {
        // Optional, used to customize the name of the message schema
        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        // Optional, used to change the message format, e.g. from JSON to base64
        [JsonProperty(PropertyName = "Format")]
        public string Format { get; set; }

        // Optional, used to replace the list of fields in the schema (the content is not merged)
        [JsonProperty(PropertyName = "Fields")]
        public IDictionary<string, string> Fields { get; set; }

        // Default constructor used by web service requests
        public DeviceModelTelemetryMessageSchemaOverride()
        {
            this.Name = null;
            this.Format = null;
            this.Fields = null;
        }

        // Map API model to service model
        public Simulation.DeviceModelTelemetryMessageSchemaOverride ToServiceModel()
        {
            if (this.IsEmpty()) return null;

            var result = new Simulation.DeviceModelTelemetryMessageSchemaOverride
            {
                Name = !string.IsNullOrEmpty(this.Name) ? this.Name : null,
                Format = null,
                Fields = null
            };

            // Map the list of fields
            if (this.Fields != null && this.Fields.Count > 0)
            {
                result.Fields = new Dictionary<string, DeviceModel.DeviceModelMessageSchemaType>();
                foreach (KeyValuePair<string, string> field in this.Fields)
                {
                    var fieldType = Enum.TryParse(field.Value, true, out DeviceModel.DeviceModelMessageSchemaType schemaType)
                        ? schemaType
                        : DeviceModel.DeviceModelMessageSchemaType.Object;
                    result.Fields.Add(field.Key, fieldType);
                }
            }

            // Map the message format
            if (!string.IsNullOrEmpty(this.Format)
                && Enum.TryParse(this.Format, true, out DeviceModel.DeviceModelMessageSchemaFormat format))
            {
                result.Format = format;
            }

            return result;
        }

        // Map service model to API model
        public static DeviceModelTelemetryMessageSchemaOverride FromServiceModel(Simulation.DeviceModelTelemetryMessageSchemaOverride value)
        {
            if (value == null) return null;

            var result = new DeviceModelTelemetryMessageSchemaOverride
            {
                Name = !string.IsNullOrEmpty(value.Name) ? value.Name : null,
                Format = value.Format?.ToString()
            };

            if (value.Fields != null && value.Fields.Count > 0)
            {
                result.Fields = new Dictionary<string, string>();
                foreach (var field in value.Fields)
                {
                    result.Fields.Add(field.Key, field.Value.ToString());
                }
            }

            return result;
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(this.Name)
                   && string.IsNullOrEmpty(this.Format)
                   && (this.Fields == null || this.Fields.Count == 0);
        }
    }
}
