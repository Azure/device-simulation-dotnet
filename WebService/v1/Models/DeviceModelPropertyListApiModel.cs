// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DeviceModelPropertyListApiModel
    {
        [JsonProperty(PropertyName = "Items")]
        public HashSet<string> items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceModelPropertyList;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/deviceModelProperties" }
        };

        private const string reportedPrefix = "Properties.Reported.";

        public DeviceModelPropertyListApiModel()
        {
            this.items = new HashSet<string>();
        }

        /// <summary>Map a service model to the corresponding API model</summary>
        public DeviceModelPropertyListApiModel(HashSet<string> value)
        {
            items = new HashSet<string>();
            foreach (string reported in value)
                items.Add(reportedPrefix + reported);
        }
    }
}
