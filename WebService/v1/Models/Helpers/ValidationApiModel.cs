// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers
{
    public class ValidationApiModel
    {
        [JsonProperty(PropertyName = "Success")]
        public bool Success { get; set; }

        [JsonProperty(PropertyName = "Messages", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Messages { get; set; }

        public ValidationApiModel()
        {
            this.Success = true;
            this.Messages = null;
        }
    }
}
