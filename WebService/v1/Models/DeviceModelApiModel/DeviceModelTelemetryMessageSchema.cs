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

        /// <summary>Map a service model to the corresponding API model</summary>
        public DeviceModelTelemetryMessageSchema(DeviceModel.DeviceModelMessageSchema schema) : this()
        {
            if (schema == null) return;

            this.Name = schema.Name;
            this.Format = schema.Format.ToString();

            foreach (var field in schema.Fields)
            {
                this.Fields.Add(field.Key, field.Value.ToString());
            }
        }
    }
}