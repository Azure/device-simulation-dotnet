// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

// TODO: tests
// TODO: handle errors
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DeviceTypeListApiModel
    {
        [JsonProperty(PropertyName = "Items")]
        public List<DeviceTypeApiModel> Items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceTypeList;" + Version.Number },
            { "$uri", "/" + Version.Path + "/devicetypes" }
        };

        public DeviceTypeListApiModel()
        {
            this.Items = new List<DeviceTypeApiModel>();
        }

        public DeviceTypeListApiModel(IEnumerable<Services.Models.DeviceType> deviceTypes)
        {
            this.Items = new List<DeviceTypeApiModel>();
            foreach (var x in deviceTypes) this.Items.Add(new DeviceTypeApiModel(x));
        }
    }
}
