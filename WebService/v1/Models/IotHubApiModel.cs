// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class IotHubApiModel
    {
        // This is the value used by the user interface
        public const string USE_DEFAULT_IOTHUB = "default";

        [JsonProperty(PropertyName = "ConnectionString")]
        public string ConnectionString { get; set; }

        public IotHubApiModel()
        {
            this.ConnectionString = USE_DEFAULT_IOTHUB;
        }

        public IotHubApiModel(string connectionString)
        {
            this.ConnectionString = connectionString;
        }
    }
}
