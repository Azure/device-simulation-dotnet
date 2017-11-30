// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

// TODO: tests
// TODO: handle errors
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DeviceModelListApiModel
    {
        [JsonProperty(PropertyName = "Items")]
        public List<DeviceModelApiModel.DeviceModelApiModel> Items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceModelList;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/devicemodels" }
        };

        public DeviceModelListApiModel()
        {
            this.Items = new List<DeviceModelApiModel.DeviceModelApiModel>();
        }

        // Map service model to API model
        public static DeviceModelListApiModel FromServiceModel(IEnumerable<DeviceModel> value)
        {
            if (value == null) return null;

            return new DeviceModelListApiModel
            {
                Items = value.Select(DeviceModelApiModel.DeviceModelApiModel.FromServiceModel).Where(x => x != null).ToList()
            };
        }
    }
}
