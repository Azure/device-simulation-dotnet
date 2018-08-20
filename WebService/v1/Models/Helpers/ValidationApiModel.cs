// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers
{
    public class ValidationApiModel
    {
        [JsonProperty(PropertyName = "IsValid")]
        public bool IsValid { get; set; }

        [JsonProperty(PropertyName = "Messages", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Messages { get; set; }

        public ValidationApiModel()
        {
            this.IsValid = true;
            this.Messages = null;
        }
    }
}
