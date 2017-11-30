// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    // SEE: <DeviceModelApiModel.DeviceModelTelemetryMessageSchema> for the original fields being overridden
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

            return new Simulation.DeviceModelTelemetryMessageSchemaOverride
            {
                Name = !string.IsNullOrEmpty(this.Name) ? this.Name : null,
                Format = !string.IsNullOrEmpty(this.Format) ? this.Format : null,
                Fields = this.Fields
            };
        }

        // Map service model to API model
        public static DeviceModelTelemetryMessageSchemaOverride FromServiceModel(Simulation.DeviceModelTelemetryMessageSchemaOverride value)
        {
            if (value == null) return null;

            return new DeviceModelTelemetryMessageSchemaOverride
            {
                Name = !string.IsNullOrEmpty(value.Name) ? value.Name : null,
                Format = !string.IsNullOrEmpty(value.Format) ? value.Format : null,
                Fields = value.Fields
            };
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(this.Name)
                   && string.IsNullOrEmpty(this.Format)
                   && (this.Fields == null || this.Fields.Count == 0);
        }
    }
}