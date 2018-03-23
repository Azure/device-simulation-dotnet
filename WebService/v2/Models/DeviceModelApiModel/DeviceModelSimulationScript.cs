// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Models.DeviceModelApiModel
{
    public class DeviceModelSimulationScript
    {
        [JsonProperty(PropertyName = "Type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "Path")]
        public string Path { get; set; }

        public DeviceModelSimulationScript()
        {
            this.Type = "javascript";
            this.Path = "scripts" + System.IO.Path.DirectorySeparatorChar;
        }

        // Map API model to service model
        public static Script ToServiceModel(DeviceModelSimulationScript value)
        {
            if (value == null) return null;

            return new Script
            {
                Type = value.Type,
                Path = value.Path
            };
        }

        // Map service model to API model
        public static DeviceModelSimulationScript FromServiceModel(Script value)
        {
            if (value == null) return null;

            return new DeviceModelSimulationScript
            {
                Type = value.Type,
                Path = value.Path
            };
        }
    }
}