// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter
{
    public class ValueApiModel
    {
        [JsonProperty("Key")]
        public string Key { get; set; }

        [JsonProperty("Data")]
        public string Data { get; set; }

        [JsonProperty("ETag")]
        public string ETag { get; set; }

        [JsonProperty("$metadata")]
        public Dictionary<string, string> Metadata;

        public ValueApiModel()
        {
            this.Metadata = new Dictionary<string, string>();
        }
    }
}
