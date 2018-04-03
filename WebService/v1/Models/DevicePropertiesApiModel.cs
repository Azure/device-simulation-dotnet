// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

// TODO: tests
// TODO: handle errors
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DevicePropertiesApiModel
    {
        [JsonProperty(PropertyName = "Reported")]
        public HashSet<string> ReportedProperties { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceProperties;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/deviceProperties" }
        };

        public DevicePropertiesApiModel()
        {
            this.ReportedProperties = new HashSet<string>();
        }

        /// <summary>Map a service model to the corresponding API model</summary>
        public DevicePropertiesApiModel(HashSet<string> deviceproerties)
        {
            this.ReportedProperties = deviceproerties;
        }
    }
}
