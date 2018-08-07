// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class DeviceApiModel
    {
        [JsonProperty(PropertyName = "Item")]
        public string Item { get; set; }

        public DeviceApiModel()
        {
            this.Item = null;
        }

        public DeviceApiModel(string item)
        {
            this.Item = item;
        }
    }
}
