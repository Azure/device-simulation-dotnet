// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.AzureManagementAdapter
{
    public class AutoScaleSettingsCreateOrUpdateRequestModel
    {
        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public Properties Properties { get; set; }
    }

    public class Properties
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("targetResourceUri")]
        public string TargetResourceUri { get; set; }

        [JsonProperty("profiles")]
        public List<Profile> Profiles { get; set; }
    }

    public class Profile
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("capacity")]
        public Capacity Capacity { get; set; }

        [JsonProperty("rules")]
        public List<object> Rules { get; set; }
    }
    
    public class Capacity
    {
        [JsonProperty("minimum")]
        public string Minimum { get; set; }

        [JsonProperty("maximum")]
        public string Maximum { get; set; }

        [JsonProperty("default")]
        public string Default { get; set; }
    }
}
