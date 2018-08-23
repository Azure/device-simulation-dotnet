// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Devices
{
    public class BatchDeleteActionApiModel
    {
        [JsonProperty(PropertyName = "DeviceIds")]
        public List<string> DeviceIds { get; set; }

        public BatchDeleteActionApiModel()
        {
            this.DeviceIds = new List<string>();
        }

        public BatchDeleteActionApiModel(List<string> items)
        {
            this.DeviceIds = items ?? new List<string>();
        }
    }
}