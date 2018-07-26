// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class SimulationScript
    {
        [JsonIgnore]
        public string ETag { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public SimulationScriptPath Path { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Modified { get; set; }
        public string Name { get; set; }

        public SimulationScript()
        {
            this.ETag = string.Empty;
            this.Id = string.Empty;
            this.Type = ScriptInterpreter.JAVASCRIPT_SCRIPT;
            this.Content = string.Empty;
            this.Path = SimulationScriptPath.Storage;
            this.Name = string.Empty;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum SimulationScriptPath
        {
            [EnumMember(Value = "Undefined")]
            Undefined = 0,

            [EnumMember(Value = "Storage")]
            Storage = 10
        }
    }
}
