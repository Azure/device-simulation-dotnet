// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DevicePropertyListApiModel
    {
        [JsonProperty(PropertyName = "Items")]
        public List<DevicePropertyApiModel> Items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DevicePropertyList;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/deviceProperties" }
        };

        public DevicePropertyListApiModel()
        {
            this.Items = new List<DevicePropertyApiModel>();
        }

        /// <summary>Map a service model to the corresponding API model</summary>
        public static DevicePropertyListApiModel FromServiceModel(List<DeviceProperty> value)
        {
            if (value == null) return null;

            return new DevicePropertyListApiModel
            {
                Items = value.Select(DevicePropertyApiModel.FromServiceModel).Where(x => x != null).ToList()
            };
        }
    }
}
