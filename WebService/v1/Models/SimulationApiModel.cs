// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class SimulationApiModel
    {
        [JsonProperty(PropertyName = "Etag")]
        public string Etag { get; set; }

        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Enabled")]
        public bool Enabled { get; set; }

        [JsonProperty(PropertyName = "DeviceTypes")]
        public List<DeviceTypeRef> DeviceTypes { get; set; }

        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "Simulation;" + v1.Version.Number },
            { "$uri", "/" + v1.Version.Path + "/simulations/" + this.Id }
        };

        public SimulationApiModel()
        {
            this.DeviceTypes = new List<DeviceTypeRef>();
        }

        /// <summary>Map a service model to the corresponding API model</summary>
        public SimulationApiModel(Services.Models.Simulation simulation)
        {
            this.DeviceTypes = new List<DeviceTypeRef>();

            this.Etag = simulation.Etag;
            this.Id = simulation.Id;
            this.Enabled = simulation.Enabled;

            foreach (var x in simulation.DeviceTypes)
            {
                var dt = new DeviceTypeRef
                {
                    Id = x.Id,
                    Count = x.Count
                };
                this.DeviceTypes.Add(dt);
            }
        }

        public class DeviceTypeRef
        {
            [JsonProperty(PropertyName = "Id")]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "Count")]
            public int Count { get; set; }
        }

        /// <summary>Map an API model to the corresponding service model</summary>
        public Services.Models.Simulation ToServiceModel(string id = "")
        {
            this.Id = id;

            var result = new Services.Models.Simulation
            {
                Etag = this.Etag,
                Id = this.Id,
                Enabled = this.Enabled
            };

            foreach (var x in this.DeviceTypes)
            {
                var dt = new Services.Models.Simulation.DeviceTypeRef
                {
                    Id = x.Id,
                    Count = x.Count
                };
                result.DeviceTypes.Add(dt);
            }

            return result;
        }
    }
}
