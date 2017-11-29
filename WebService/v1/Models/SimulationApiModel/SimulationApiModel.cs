// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class SimulationApiModel
    {
        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";

        private long version;
        private DateTimeOffset created;
        private DateTimeOffset modified;

        [JsonProperty(PropertyName = "ETag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Enabled")]
        public bool? Enabled { get; set; }

        [JsonProperty(PropertyName = "IoTHub")]
        public SimulationIotHub IotHub { get; set; }

        [JsonProperty(PropertyName = "StartTime", NullValueHandling = NullValueHandling.Ignore)]
        public string StartTime { get; set; }

        [JsonProperty(PropertyName = "EndTime", NullValueHandling = NullValueHandling.Ignore)]
        public string EndTime { get; set; }

        [JsonProperty(PropertyName = "DeviceModels")]
        public IList<SimulationDeviceModelRef> DeviceModels { get; set; }

        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "Simulation;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/simulations/" + this.Id },
            { "$version", this.version.ToString() },
            { "$created", this.created.ToString(DATE_FORMAT) },
            { "$modified", this.modified.ToString(DATE_FORMAT) }
        };

        // Default constructor used by web service requests
        public SimulationApiModel()
        {
            this.Id = string.Empty;

            // When unspecified, a simulation is enabled
            this.Enabled = true;
            this.IotHub = null;
            this.StartTime = null;
            this.EndTime = null;
            this.DeviceModels = new List<SimulationDeviceModelRef>();
        }

        // Map API model to service model
        public Simulation ToServiceModel(string id = "")
        {
            this.Id = id;

            var now = DateTimeOffset.UtcNow;

            var result = new Simulation
            {
                ETag = this.ETag,
                Id = this.Id,
                // When unspecified, a simulation is enabled
                Enabled = this.Enabled ?? true,
                StartTime = DateHelper.ParseDateExpression(this.StartTime, now),
                EndTime = DateHelper.ParseDateExpression(this.EndTime, now),
                IotHubConnectionString = SimulationIotHub.ToServiceModel(this.IotHub),
                DeviceModels = this.DeviceModels?.Select(x => x.ToServiceModel()).ToList()
            };

            if (result.StartTime.HasValue && result.EndTime.HasValue
                                          && result.StartTime.Value.Ticks >= result.EndTime.Value.Ticks)
            {
                throw new InvalidSimulationSchedulingException("The simulation End Time must be after the Start Time");
            }

            return result;
        }

        // Map service model to API model
        public static SimulationApiModel FromServiceModel(Simulation value)
        {
            if (value == null) return null;

            var result = new SimulationApiModel
            {
                ETag = value.ETag,
                Id = value.Id,
                Enabled = value.Enabled,
                IotHub = new SimulationIotHub(value.IotHubConnectionString)
            };

            // Ignore the date if the simulation doesn't have a start time
            if (value.StartTime.HasValue && !value.StartTime.Value.Equals(DateTimeOffset.MinValue))
            {
                result.StartTime = value.StartTime?.ToString(DATE_FORMAT);
            }

            // Ignore the date if the simulation doesn't have an end time
            if (value.EndTime.HasValue && !value.EndTime.Value.Equals(DateTimeOffset.MaxValue))
            {
                result.EndTime = value.EndTime?.ToString(DATE_FORMAT);
            }

            result.DeviceModels = SimulationDeviceModelRef.FromServiceModel(value.DeviceModels);
            result.version = value.Version;
            result.created = value.Created;
            result.modified = value.Modified;

            return result;
        }
    }
}
