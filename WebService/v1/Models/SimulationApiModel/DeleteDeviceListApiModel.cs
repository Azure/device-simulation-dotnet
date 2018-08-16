// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic; 
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class DeleteDeviceListApiModel
    {
        [JsonProperty(PropertyName = "DeviceIds")]
        public List<string> DeviceIds { get; set; }

        [JsonProperty(PropertyName = "IsCustom")]
        public bool IsCustom { get; set; }

        public DeleteDeviceListApiModel()
        {
            this.DeviceIds = null;
            this.IsCustom = false;
        }

        public DeleteDeviceListApiModel(List<string> items, bool isCustom)
        {
            this.DeviceIds = items;
            this.IsCustom = isCustom;
        }
    }
}
