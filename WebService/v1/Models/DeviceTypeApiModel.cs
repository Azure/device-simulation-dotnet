// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

// TODO: tests
// TODO: handle errors
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DeviceTypeApiModel
    {
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Version")]
        public string Version { get; set; }

        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "Description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "Protocol")]
        public string Protocol { get; set; }

        [JsonProperty(PropertyName = "DeviceState")]
        public InternalStateApiModel DeviceState { get; set; }

        [JsonProperty(PropertyName = "Telemetry")]
        public IList<DeviceTypeMessageApiModel> Telemetry { get; set; }

        [JsonProperty(PropertyName = "CloudToDeviceMethods")]
        public IDictionary<string, ScriptApiModel> CloudToDeviceMethods { get; set; }

        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceType;" + v1.Version.Number },
            { "$uri", "/" + v1.Version.Path + "/devicetypes/" + this.Id }
        };

        public DeviceTypeApiModel()
        {
            this.DeviceState = new InternalStateApiModel();
            this.Telemetry = new List<DeviceTypeMessageApiModel>();
            this.CloudToDeviceMethods = new Dictionary<string, ScriptApiModel>();
        }

        /// <summary>Map a service model to the corresponding API model</summary>
        public DeviceTypeApiModel(DeviceType type) : this()
        {
            if (type == null) return;

            this.Id = type.Id;
            this.Version = type.Version;
            this.Name = type.Name;
            this.Description = type.Description;
            this.Protocol = type.Protocol.ToString();
            this.DeviceState = new InternalStateApiModel(type.DeviceState);

            foreach (var message in type.Telemetry)
            {
                this.Telemetry.Add(new DeviceTypeMessageApiModel(message));
            }

            foreach (var method in type.CloudToDeviceMethods)
            {
                this.CloudToDeviceMethods.Add(method.Key, new ScriptApiModel(method.Value));
            }
        }

        public class InternalStateApiModel
        {
            [JsonProperty(PropertyName = "Initial")]
            public Dictionary<string, object> Initial { get; set; }

            [JsonProperty(PropertyName = "SimulationInterval")]
            public string SimulationInterval { get; set; }

            [JsonProperty(PropertyName = "SimulationScript")]
            public ScriptApiModel SimulationScript { get; set; }

            public InternalStateApiModel()
            {
                this.Initial = new Dictionary<string, object>();
                this.SimulationInterval = "00:00:00";
                this.SimulationScript = new ScriptApiModel();
            }

            /// <summary>Map a service model to the corresponding API model</summary>
            public InternalStateApiModel(DeviceType.InternalState state) : this()
            {
                if (state == null) return;

                this.Initial = state.Initial;
                this.SimulationInterval = state.SimulationInterval.ToString("c");
                this.SimulationScript = new ScriptApiModel(state.SimulationScript);
            }
        }

        public class ScriptApiModel
        {
            [JsonProperty(PropertyName = "Type")]
            public string Type { get; set; }

            [JsonProperty(PropertyName = "Path")]
            public string Path { get; set; }

            public ScriptApiModel()
            {
                this.Type = "javascript";
                this.Path = "scripts" + System.IO.Path.DirectorySeparatorChar;
            }

            /// <summary>Map a service model to the corresponding API model</summary>
            public ScriptApiModel(Script script) : this()
            {
                if (script == null) return;

                this.Type = script.Type;
                this.Path = script.Path;
            }
        }

        public class DeviceTypeMessageApiModel
        {
            [JsonProperty(PropertyName = "Interval")]
            public string Interval { get; set; }

            [JsonProperty(PropertyName = "MessageTemplate")]
            public string MessageTemplate { get; set; }

            [JsonProperty(PropertyName = "MessageSchema")]
            public DeviceTypeMessageSchemaApiModel MessageSchema { get; set; }

            public DeviceTypeMessageApiModel()
            {
                this.Interval = "00:00:00";
                this.MessageTemplate = string.Empty;
                this.MessageSchema = new DeviceTypeMessageSchemaApiModel();
            }

            /// <summary>Map a service model to the corresponding API model</summary>
            public DeviceTypeMessageApiModel(DeviceType.DeviceTypeMessage message) : this()
            {
                if (message == null) return;

                this.Interval = message.Interval.ToString("c");
                this.MessageTemplate = message.MessageTemplate;
                this.MessageSchema = new DeviceTypeMessageSchemaApiModel(message.MessageSchema);
            }
        }

        public class DeviceTypeMessageSchemaApiModel
        {
            [JsonProperty(PropertyName = "Name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "Format")]
            public string Format { get; set; }

            [JsonProperty(PropertyName = "Fields")]
            public IDictionary<string, string> Fields { get; set; }

            public DeviceTypeMessageSchemaApiModel()
            {
                this.Name = string.Empty;
                this.Format = "JSON";
                this.Fields = new Dictionary<string, string>();
            }

            /// <summary>Map a service model to the corresponding API model</summary>
            public DeviceTypeMessageSchemaApiModel(DeviceType.DeviceTypeMessageSchema schema) : this()
            {
                if (schema == null) return;

                this.Name = schema.Name;
                this.Format = schema.Format.ToString();

                foreach (var field in schema.Fields)
                {
                    this.Fields.Add(field.Key, field.Value.ToString());
                }
            }
        }
    }
}
