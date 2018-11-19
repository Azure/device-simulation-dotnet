// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class StatusServiceModel
    {
        [JsonProperty(PropertyName = "Status")]
        public StatusResultServiceModel Status { get; set; }

        [JsonProperty(PropertyName = "Properties")]
        public Dictionary<string, string> Properties { get; set; }

        [JsonProperty(PropertyName = "Dependencies")]
        public Dictionary<string, StatusResultServiceModel> Dependencies { get; set; }

        public StatusServiceModel(bool isHealthy, string message)
        {
            this.Status = new StatusResultServiceModel(isHealthy, message);
            this.Dependencies = new Dictionary<string, StatusResultServiceModel>();
            this.Properties = new Dictionary<string, string>();
        }
    }
}
