// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class DevicesPartition
    {
        [JsonProperty(Order = 1)]
        public string Id { get; set; }

        [JsonProperty(Order = 10)]
        public string SimulationId { get; set; }

        [JsonProperty(Order = 20)]
        public int Size { get; set; }

        [JsonProperty(Order = 100)]
        public Dictionary<string, List<string>> DeviceIdsByModel { get; set; }
    }
}
