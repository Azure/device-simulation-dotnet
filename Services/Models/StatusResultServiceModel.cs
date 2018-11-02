// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class StatusResultServiceModel
    {
        [JsonProperty(PropertyName = "IsHealthy")]
        public bool IsHealthy { get; set; }

        [JsonProperty(PropertyName = "Message")]
        public string Message { get; set; }

        public StatusResultServiceModel(StatusResultServiceModel result)
        {
            IsHealthy = result.IsHealthy;
            Message = result.Message;
        }

        [JsonConstructor]
        public StatusResultServiceModel(bool isHealthy, string message)
        {
            IsHealthy = isHealthy;
            Message = message;
        }
    }
}
