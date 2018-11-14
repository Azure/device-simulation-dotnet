// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    // TODO: delete this model when the application supports multiple connection strings,
    //       see SimulationsController and Devices.InitAsync
    public class IoTHubApiModel
    {
        [JsonProperty(PropertyName = "ConnectionString")]
        public string ConnectionString { get; set; }
    }
}
