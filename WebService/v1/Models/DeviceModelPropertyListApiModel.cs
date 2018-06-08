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
        public List<DeviceModelPropertyApiModel> Items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceModelPropertyList;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/devicemodelproperties" }
        };

        public DeviceModelPropertyListApiModel()
        {
            this.Items = new List<DeviceModelPropertyApiModel>();
        }

        /// <summary>Map a service model to the corresponding API model</summary>
        public static DeviceModelPropertyListApiModel FromServiceModel(List<DeviceProperty> value)
        {
            if (value == null) return null;

            return new DeviceModelPropertyListApiModel
            {
                Items = value
                    .Select(DeviceModelPropertyApiModel.FromServiceModel)
                    .Where(x => x != null).ToList()
            };
        }
    }
}
