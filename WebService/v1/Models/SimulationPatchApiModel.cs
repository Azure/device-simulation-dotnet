// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class SimulationPatchApiModel
    {
        [JsonProperty(PropertyName = "Etag")]
        public string Etag { get; set; }

        [JsonProperty(PropertyName = "Enabled")]
        public bool? Enabled { get; set; }

        public SimulationPatchApiModel()
        {
            this.Enabled = null;
        }

        /// <summary>Map an API model to the corresponding service model</summary>
        public Services.Models.SimulationPatch ToServiceModel(string id)
        {
            return new Services.Models.SimulationPatch
            {
                Etag = this.Etag,
                Id = id,
                Enabled = this.Enabled
            };
        }
    }
}
