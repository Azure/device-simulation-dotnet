// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter
{
    public class ValueListApiModel
    {
        public List<ValueApiModel> Items { get; set; }

        [JsonProperty("$metadata")]
        public Dictionary<string, string> Metadata { get; set; }

        public ValueListApiModel()
        {
            this.Items = new List<ValueApiModel>();
        }
    }
}
