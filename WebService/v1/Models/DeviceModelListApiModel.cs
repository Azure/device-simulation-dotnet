// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

// TODO: tests
// TODO: handle errors
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DeviceModelListApiModel
    {
        [JsonProperty(PropertyName = "Items")]
        public List<DeviceModelApiModel> Items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceModelList;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/devicemodels" }
        };

        public DeviceModelListApiModel()
        {
            this.Items = new List<DeviceModelApiModel>();
        }

        /// <summary>Map a service model to the corresponding API model</summary>
        public DeviceModelListApiModel(IEnumerable<Services.Models.DeviceModel> deviceModels)
        {
            this.Items = new List<DeviceModelApiModel>();
            foreach (var x in deviceModels) this.Items.Add(new DeviceModelApiModel(x));
        }
    }
}
