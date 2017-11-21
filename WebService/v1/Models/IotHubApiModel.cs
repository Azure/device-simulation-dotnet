// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class IotHubApiModel
    {
        private const string USE_LOCAL_IOTHUB = "pre-provisioned";
        private string iotHubConnectionString;

        [JsonProperty(PropertyName = "ConnectionString")]
        public string ConnectionString
        {
            get => this.iotHubConnectionString;
            set =>
                // remove and securely store senstive key information
                // from IoTHub connection string
                // if value is "pre-provisioned" will use hub info
                // in PCS_IOTHUB_CONNSTRING
                this.iotHubConnectionString =
                    IotHubConnectionStringManager.StoreAndRedact(value);
        }

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
