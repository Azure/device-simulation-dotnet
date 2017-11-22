// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class IotHubApiModel
    {
        private const string USE_LOCAL_IOTHUB = "pre-provisioned";

        [JsonProperty(PropertyName = "ConnectionString")]
        public string ConnectionString { get; set; }

        public IotHubApiModel()
        {
            this.ConnectionString = USE_LOCAL_IOTHUB;
        }

        public IotHubApiModel(string connectionString)
        {
            this.ConnectionString = connectionString;
        }
    }
}
