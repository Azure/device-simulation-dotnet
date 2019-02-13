// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelScriptApiModel
{
    public class DeviceModelScriptApiModel
    {
        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";

        private DateTimeOffset created;
        private DateTimeOffset modified;

        [JsonProperty(PropertyName = "ETag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "Content")]
        public string Content { get; set; }

        [JsonProperty(PropertyName = "Path")]
        public string Path { get; set; }

        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceModelScript;" + v1.Version.NUMBER },
            { "$uri", "/" + v1.Version.PATH + "/simulationscripts/" + this.Id },
            { "$created", this.created.ToString(DATE_FORMAT) },
            { "$modified", this.modified.ToString(DATE_FORMAT) }
        };

        public DeviceModelScriptApiModel()
        {
            this.ETag = string.Empty;
            this.Id = string.Empty;
            this.Type = string.Empty;
            this.Content = string.Empty;
            this.Path = string.Empty;
            this.Name = string.Empty;
        }

        // Map API model to service model
        public Services.Models.DataFile ToServiceModel()
        {
            return new Services.Models.DataFile
            {
                ETag = this.ETag,
                Id = this.Id,
                Type = this.Type,
                Path = (Services.Models.DataFile.FilePath)Enum.Parse(typeof(Services.Models.DataFile.FilePath), this.Path, true),
                Content = this.Content,
                Name = this.Name
            };
        }

        // Map service model to API model
        public static DeviceModelScriptApiModel FromServiceModel(Services.Models.DataFile value)
        {
            if (value == null) return null;

            var result = new DeviceModelScriptApiModel
            {
                ETag = value.ETag,
                Id = value.Id,
                Type = value.Type,
                created = value.Created,
                modified = value.Modified,
                Path = value.Path.ToString(),
                Content = value.Content,
                Name = value.Name
            };

            return result;
        }
    }
}
