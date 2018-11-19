// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class SimulationPatchApiModel
    {
        [JsonProperty(PropertyName = "ETag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "Enabled")]
        public bool? Enabled { get; set; }

        [JsonProperty(PropertyName = "DeleteDevicesOnce")]
        public bool? DeleteDevicesOnce { get; set; }

        public SimulationPatchApiModel()
        {
            this.Enabled = null;
            this.DeleteDevicesOnce = false;
        }

        /// <summary>Map an API model to the corresponding service model</summary>
        public Services.Models.SimulationPatch ToServiceModel(string id)
        {
            return new Services.Models.SimulationPatch
            {
                ETag = this.ETag,
                Id = id,
                Enabled = this.Enabled,
                DeleteDevicesOnce = this.DeleteDevicesOnce
            };
        }
    }
}
