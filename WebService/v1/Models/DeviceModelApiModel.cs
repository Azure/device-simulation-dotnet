// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

// TODO: tests
// TODO: handle errors
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DeviceModelApiModel
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

        [JsonProperty(PropertyName = "Simulation")]
        public StateSimulationApiModel Simulation { get; set; }

        [JsonProperty(PropertyName = "Properties")]
        public IDictionary<string, object> Properties { get; set; }

        [JsonProperty(PropertyName = "Telemetry")]
        public IList<DeviceModelMessageApiModel> Telemetry { get; set; }

        [JsonProperty(PropertyName = "CloudToDeviceMethods")]
        public IDictionary<string, ScriptApiModel> CloudToDeviceMethods { get; set; }

        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceModel;" + v1.Version.NUMBER },
            { "$uri", "/" + v1.Version.PATH + "/devicemodels/" + this.Id }
        };

        public DeviceModelApiModel()
        {
            this.Simulation = new StateSimulationApiModel();
            this.Telemetry = new List<DeviceModelMessageApiModel>();
            this.Properties = new Dictionary<string, object>();
            this.CloudToDeviceMethods = new Dictionary<string, ScriptApiModel>();
        }

        /// <summary>Map a service model to the corresponding API model</summary>
        public DeviceModelApiModel(DeviceModel model) : this()
        {
            if (model == null) return;

            this.Id = model.Id;
            this.Version = model.Version;
            this.Name = model.Name;
            this.Description = model.Description;
            this.Protocol = model.Protocol.ToString();
            this.Simulation = new StateSimulationApiModel(model.Simulation);

            foreach (var property in model.Properties)
            {
                this.Properties.Add(property.Key, property.Value);
            }

            foreach (var message in model.Telemetry)
            {
                this.Telemetry.Add(new DeviceModelMessageApiModel(message));
            }

            foreach (var method in model.CloudToDeviceMethods)
            {
                this.CloudToDeviceMethods.Add(method.Key, new ScriptApiModel(method.Value));
            }
        }

        public class StateSimulationApiModel
        {
            [JsonProperty(PropertyName = "InitialState")]
            public Dictionary<string, object> Initial { get; set; }

            [JsonProperty(PropertyName = "Script")]
            public ScriptApiModel SimulationScript { get; set; }

            public StateSimulationApiModel()
            {
                this.Initial = new Dictionary<string, object>();
                this.SimulationScript = new ScriptApiModel();
            }

            /// <summary>Map a service model to the corresponding API model</summary>
            public StateSimulationApiModel(DeviceModel.StateSimulation state) : this()
            {
                if (state == null) return;

                this.Initial = state.InitialState;
                this.SimulationScript = new ScriptApiModel(state.Script);
            }
        }

        public class ScriptApiModel
        {
            [JsonProperty(PropertyName = "Type")]
            public string Type { get; set; }

            [JsonProperty(PropertyName = "Path")]
            public string Path { get; set; }

            [JsonProperty(PropertyName = "Interval", NullValueHandling = NullValueHandling.Ignore)]
            public string Interval { get; set; }

            public ScriptApiModel()
            {
                this.Type = "javascript";
                this.Path = "scripts" + System.IO.Path.DirectorySeparatorChar;
                this.Interval = null;
            }

            /// <summary>Map a service model to the corresponding API model</summary>
            public ScriptApiModel(Script script) : this()
            {
                if (script == null) return;

                this.Type = script.Type;
                this.Path = script.Path;
                this.Interval = script.Interval.ToString("c");
            }
        }

        public class DeviceModelMessageApiModel
        {
            [JsonProperty(PropertyName = "Interval")]
            public string Interval { get; set; }

            [JsonProperty(PropertyName = "MessageTemplate")]
            public string MessageTemplate { get; set; }

            [JsonProperty(PropertyName = "MessageSchema")]
            public DeviceModelMessageSchemaApiModel MessageSchema { get; set; }

            public DeviceModelMessageApiModel()
            {
                this.Interval = "00:00:00";
                this.MessageTemplate = string.Empty;
                this.MessageSchema = new DeviceModelMessageSchemaApiModel();
            }

            /// <summary>Map a service model to the corresponding API model</summary>
            public DeviceModelMessageApiModel(DeviceModel.DeviceModelMessage message) : this()
            {
                if (message == null) return;

                this.Interval = message.Interval.ToString("c");
                this.MessageTemplate = message.MessageTemplate;
                this.MessageSchema = new DeviceModelMessageSchemaApiModel(message.MessageSchema);
            }
        }

        public class DeviceModelMessageSchemaApiModel
        {
            [JsonProperty(PropertyName = "Name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "Format")]
            public string Format { get; set; }

            [JsonProperty(PropertyName = "Fields")]
            public IDictionary<string, string> Fields { get; set; }

            public DeviceModelMessageSchemaApiModel()
            {
                this.Name = string.Empty;
                this.Format = "JSON";
                this.Fields = new Dictionary<string, string>();
            }

            /// <summary>Map a service model to the corresponding API model</summary>
            public DeviceModelMessageSchemaApiModel(DeviceModel.DeviceModelMessageSchema schema) : this()
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
