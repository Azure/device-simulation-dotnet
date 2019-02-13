// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class DataFile
    {
        [JsonIgnore]
        public string ETag { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public FilePath Path { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Modified { get; set; }

        public DataFile()
        {
            this.ETag = string.Empty;
            this.Id = string.Empty;
            this.Type = string.Empty;
            this.Content = string.Empty;
            this.Path = FilePath.Storage;
            this.Name = string.Empty;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum FilePath
        {
            [EnumMember(Value = "Undefined")]
            Undefined = 0,

            [EnumMember(Value = "Storage")]
            Storage = 10
        }
    }
}
