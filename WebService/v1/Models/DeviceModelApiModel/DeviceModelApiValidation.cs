// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel
{
    public class DeviceModelApiValidation
    {
        [JsonProperty(PropertyName = "Success")]
        public bool Success { get; set; }

        [JsonProperty(PropertyName = "Messages", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Messages { get; set; }

        public DeviceModelApiValidation()
        {
            this.Success = true;
            this.Messages = null;
        }
    }
}
