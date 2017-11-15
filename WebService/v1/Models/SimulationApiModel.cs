// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class SimulationApiModel
    {
        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";
        private readonly long version;
        private DateTimeOffset created;
        private DateTimeOffset modified;

        [JsonProperty(PropertyName = "ETag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Enabled")]
        public bool? Enabled { get; set; }

        [JsonProperty(PropertyName = "StartTime", NullValueHandling = NullValueHandling.Ignore)]
        public string StartTime { get; set; }

        [JsonProperty(PropertyName = "EndTime", NullValueHandling = NullValueHandling.Ignore)]
        public string EndTime { get; set; }

        [JsonProperty(PropertyName = "DeviceModels")]
        public List<DeviceModelRef> DeviceModels { get; set; }

        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "Simulation;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/simulations/" + this.Id },
            { "$version", this.version.ToString() },
            { "$created", this.created.ToString(DATE_FORMAT) },
            { "$modified", this.modified.ToString(DATE_FORMAT) }
        };

        public SimulationApiModel()
        {
            this.Id = string.Empty;

            // When unspecified, a simulation is enabled
            this.Enabled = true;

            this.StartTime = null;
            this.EndTime = null;
            this.DeviceModels = new List<DeviceModelRef>();
        }

        /// <summary>Map a service model to the corresponding API model</summary>
        public SimulationApiModel(Simulation simulation) : this()
        {
            this.ETag = simulation.ETag;
            this.Id = simulation.Id;
            this.Enabled = simulation.Enabled;

            // Ignore the date if the simulation doesn't have a start time
            if (simulation.StartTime.HasValue && !simulation.StartTime.Value.Equals(DateTimeOffset.MinValue))
            {
                this.StartTime = simulation.StartTime?.ToString(DATE_FORMAT);
            }

            // Ignore the date if the simulation doesn't have an end time
            if (simulation.EndTime.HasValue && !simulation.EndTime.Value.Equals(DateTimeOffset.MaxValue))
            {
                this.EndTime = simulation.EndTime?.ToString(DATE_FORMAT);
            }

            foreach (var x in simulation.DeviceModels)
            {
                var dt = new DeviceModelRef
                {
                    Id = x.Id,
                    Count = x.Count
                };
                this.DeviceModels.Add(dt);
            }

            this.version = simulation.Version;
            this.created = simulation.Created;
            this.modified = simulation.Modified;
        }

        public class DeviceModelRef
        {
            [JsonProperty(PropertyName = "Id")]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "Count")]
            public int Count { get; set; }
        }

        /// <summary>Map an API model to the corresponding service model</summary>
        /// <param name="id">The simulation ID when using PUT/PATCH, empty otherwise</param>
        public Simulation ToServiceModel(string id = "")
        {
            this.Id = id;

            var result = new Simulation
            {
                ETag = this.ETag,
                Id = this.Id,
                StartTime = DateHelper.ParseDate(this.StartTime),
                EndTime = DateHelper.ParseDate(this.EndTime),

                // When unspecified, a simulation is enabled
                Enabled = this.Enabled ?? true
            };

            foreach (var x in this.DeviceModels)
            {
                var dt = new Simulation.DeviceModelRef
                {
                    Id = x.Id,
                    Count = x.Count
                };
                result.DeviceModels.Add(dt);
            }

            return result;
        }
    }
}
