// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel
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

        /// <summary>Map a service model to the corresponding API model</summary>
        public DeviceModelSimulationScript(Script script) : this()
        {
            if (script == null) return;

            this.Type = script.Type;
            this.Path = script.Path;
        }
    }
}