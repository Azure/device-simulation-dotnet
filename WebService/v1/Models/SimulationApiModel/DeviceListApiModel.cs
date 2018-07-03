// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic; 
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class DeviceListApiModel
    {
        [JsonProperty(PropertyName = "Items")]
        public List<string> Items { get; set; }

        public DeviceListApiModel()
        {
            this.Items = null;
        }

        public DeviceListApiModel(List<string> items)
        {
            this.Items = items;
        }
    }
}
