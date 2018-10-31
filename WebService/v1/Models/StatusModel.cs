// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class StatusModel
    {
        [JsonProperty(PropertyName = "IsHealthy", Order = 10)]
        public bool IsHealthy { get; set; }

        [JsonProperty(PropertyName = "Message", Order = 20)]
        public string Message { get; set; }
    }
}
