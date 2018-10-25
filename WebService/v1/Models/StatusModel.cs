// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class StatusModel
    {
        [JsonProperty(PropertyName = "Message", Order = 10)]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "IsConnected", Order = 20)]
        public bool IsConnected { get; set; }
    }
}
