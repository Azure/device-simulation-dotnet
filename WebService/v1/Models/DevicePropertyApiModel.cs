// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DevicePropertyApiModel
    {
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        public DevicePropertyApiModel()
        {
            this.Id = string.Empty;
        }

        public static DevicePropertyApiModel FromServiceModel(DeviceProperty value)
        {
            if (value == null) return null;

            return new DevicePropertyApiModel
            {
                Id = value.Id
            };
        }
    }
}