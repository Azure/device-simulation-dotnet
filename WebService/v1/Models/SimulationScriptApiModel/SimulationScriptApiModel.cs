// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationScriptApiModel
{
    public class SimulationScriptApiModel
    {
        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";

        private DateTimeOffset created;
        private DateTimeOffset modified;

        [JsonProperty(PropertyName = "ETag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "Content")]
        public string Content { get; set; }

        [JsonProperty(PropertyName = "Path")]
        public string Path { get; set; }

        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "SimulationScript;" + v1.Version.NUMBER },
            { "$uri", "/" + v1.Version.PATH + "/simulationscripts/" + this.Id },
            { "$created", this.created.ToString(DATE_FORMAT) },
            { "$modified", this.modified.ToString(DATE_FORMAT) }
        };

        public SimulationScriptApiModel()
        {
            this.ETag = string.Empty;
            this.Id = string.Empty;
            this.Type = string.Empty;
            this.Content = string.Empty;
            this.Path = string.Empty;
            this.Name = string.Empty;
        }

        // Map API model to service model
        public SimulationScript ToServiceModel()
        {
            return new SimulationScript
            {
                ETag = this.ETag,
                Id = this.Id,
                Type = this.Type,
                Path = (SimulationScript.SimulationScriptPath)Enum.Parse(typeof(SimulationScript.SimulationScriptPath), this.Path, true),
                Content = this.Content,
                Name = this.Name
            };
        }

        // Map service model to API model
        public static SimulationScriptApiModel FromServiceModel(SimulationScript value)
        {
            if (value == null) return null;

            var result = new SimulationScriptApiModel
            {
                ETag = value.ETag,
                Id = value.Id,
                Type = value.Type,
                created = value.Created,
                modified = value.Modified,
                Path = value.Path.ToString(),
                Content = value.Content,
                Name = value.Name
            };

            return result;
        }
    }
}
