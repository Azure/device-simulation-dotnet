// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DeviceListApiModel
    {
        private string prev;
        private string next;

        [JsonProperty(PropertyName = "Total")]
        public int Total { get; set; }

        [JsonProperty(PropertyName = "Items")]
        public List<Devices.DeviceApiModel> Items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
           { "$type", "DeviceList;" + Version.NUMBER },
           { "$uri", "/" + Version.PATH + "/devices" },
           { "linkToPrev", this.prev == null ? null : "/" + Version.PATH + "/devices" + this.prev },
           { "linkToNext", this.next == null ? null : "/" + Version.PATH + "/devices" + this.next }
        };

        public DeviceListApiModel()
        {
            this.Total = 0;
            this.Items = new List<Devices.DeviceApiModel>();
        }

        public DeviceListApiModel(IEnumerable<Services.Models.Device> devices)
        {
            this.Items = new List<Devices.DeviceApiModel>();
            foreach(var x in devices)
            {
                this.Items.Add(Devices.DeviceApiModel.FromServiceModel(x));
            }
        }

        public static DeviceListApiModel FromServiceModel(DeviceList deviceList)
        {
            var result = new DeviceListApiModel();
            result.Total = deviceList.Total;
            result.prev = deviceList.Prev;
            result.next = deviceList.Next;
            
            if (deviceList.Items != null)
            {
                foreach(var x in deviceList.Items)
                {
                    result.Items.Add(Devices.DeviceApiModel.FromServiceModel(x));
                }
            }

            return result;
        }
    }
}
