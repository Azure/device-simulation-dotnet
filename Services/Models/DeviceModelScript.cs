// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Runtime.Serialization;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class DeviceModelScript
    {
        [JsonIgnore]
        public string ETag { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public DeviceModelScriptPath Path { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Modified { get; set; }

        public DeviceModelScript()
        {
            this.ETag = string.Empty;
            this.Id = string.Empty;
            this.Type = ScriptInterpreter.JAVASCRIPT_SCRIPT;
            this.Content = string.Empty;
            this.Path = DeviceModelScriptPath.Storage;
            this.Name = string.Empty;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum DeviceModelScriptPath
        {
            [EnumMember(Value = "Undefined")]
            Undefined = 0,

            [EnumMember(Value = "Storage")]
            Storage = 10
        }
    }
}
