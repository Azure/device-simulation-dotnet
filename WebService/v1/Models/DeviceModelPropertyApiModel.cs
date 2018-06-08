// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DeviceModelPropertyApiModel
    {
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        public DeviceModelPropertyApiModel()
        {
            this.Id = string.Empty;
        }

        public static DeviceModelPropertyApiModel FromServiceModel(DeviceProperty value)
        {
            if (value == null) return null;

            return new DeviceModelPropertyApiModel
            {
                Id = value.Id
            };
        }
    }
}