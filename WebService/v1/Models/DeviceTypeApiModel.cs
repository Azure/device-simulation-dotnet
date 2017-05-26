// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// TODO: tests
// TODO: handle errors
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DeviceTypeApiModel
    {
        public DeviceTypeApiModel()
        {
            this.DeviceBehavior = new Dictionary<string, DeviceTypeFunction>();
            this.Methods = new Dictionary<string, DeviceTypeMethod>();
        }

        /// <summary>Map a service model to the corresponding API model</summary>
        public DeviceTypeApiModel(Services.Models.DeviceType type)
        {
            this.DeviceBehavior = new Dictionary<string, DeviceTypeFunction>();
            this.Methods = new Dictionary<string, DeviceTypeMethod>();

            if (type == null) return;

            this.Id = type.Id;
            this.Version = type.Version;
            this.Name = type.Name;
            this.Description = type.Description;
            this.Protocol = type.Protocol.ToString();
            this.Telemetry = new DeviceTypeTelemetry(type.Telemetry);

            foreach (var x in type.CloudToDeviceMethods) this.Methods.Add(x.Key, new DeviceTypeMethod(x.Value));
            foreach (var x in type.DeviceBehavior) this.DeviceBehavior.Add(x.Key, new DeviceTypeFunction(x.Value));
        }

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

        [JsonProperty(PropertyName = "DeviceBehavior")]
        public IDictionary<string, DeviceTypeFunction> DeviceBehavior { get; set; }

        [JsonProperty(PropertyName = "Telemetry")]
        public DeviceTypeTelemetry Telemetry { get; set; }

        [JsonProperty(PropertyName = "Methods")]
        public IDictionary<string, DeviceTypeMethod> Methods { get; set; }

        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceType;" + v1.Version.Number },
            { "$uri", "/" + v1.Version.Path + "/devicetypes/" + this.Id }
        };

        public class DeviceTypeFunction
        {
            public DeviceTypeFunction()
            {
            }

            /// <summary>Map a service model to the corresponding API model</summary>
            public DeviceTypeFunction(Services.Models.DeviceType.DeviceTypeFunction function)
            {
                if (function == null) return;

                this.Type = function.Type;
                this.Path = function.Path;
            }

            [JsonProperty(PropertyName = "Type")]
            public string Type { get; set; }

            [JsonProperty(PropertyName = "Path")]
            public string Path { get; set; }
        }

        public class DeviceTypeMethod
        {
            public DeviceTypeMethod()
            {
            }

            /// <summary>Map a service model to the corresponding API model</summary>
            public DeviceTypeMethod(Services.Models.DeviceType.DeviceTypeMethod method)
            {
                if (method == null) return;

                this.Actions = method.Actions;
            }

            [JsonProperty(PropertyName = "Actions")]
            public IList<string> Actions { get; set; }
        }

        public class DeviceTypeTelemetry
        {
            public DeviceTypeTelemetry()
            {
                this.Messages = new List<DeviceTypeMessage>();
            }

            /// <summary>Map a service model to the corresponding API model</summary>
            public DeviceTypeTelemetry(Services.Models.DeviceType.DeviceTypeTelemetry telemetry)
            {
                this.Messages = new List<DeviceTypeMessage>();

                if (telemetry == null) return;

                foreach (var x in telemetry.Messages) this.Messages.Add(new DeviceTypeMessage(x));
            }

            [JsonProperty(PropertyName = "Messages")]
            public IList<DeviceTypeMessage> Messages { get; set; }
        }

        public class DeviceTypeMessage
        {
            public DeviceTypeMessage()
            {
            }

            /// <summary>Map a service model to the corresponding API model</summary>
            public DeviceTypeMessage(Services.Models.DeviceType.DeviceTypeMessage message)
            {
                if (message == null) return;

                this.Interval = Convert.ToInt64(message.Interval.TotalMilliseconds);
                this.Message = message.Message;
                this.MessageSchema = new DeviceTypeMessageSchema(message.MessageSchema);
            }

            [JsonProperty(PropertyName = "Interval")]
            public long Interval { get; set; }

            [JsonProperty(PropertyName = "Message")]
            public string Message { get; set; }

            [JsonProperty(PropertyName = "MessageSchema")]
            public DeviceTypeMessageSchema MessageSchema { get; set; }
        }

        public class DeviceTypeMessageSchema
        {
            public DeviceTypeMessageSchema()
            {
                this.Fields = new Dictionary<string, string>();
            }

            /// <summary>Map a service model to the corresponding API model</summary>
            public DeviceTypeMessageSchema(Services.Models.DeviceType.DeviceTypeMessageSchema schema)
            {
                this.Fields = new Dictionary<string, string>();

                if (schema == null) return;

                this.Format = schema.Format.ToString();

                foreach (var x in schema.Fields) this.Fields.Add(x.Key, x.Value.ToString());
            }

            [JsonProperty(PropertyName = "Format")]
            public string Format { get; set; }

            [JsonProperty(PropertyName = "Fields")]
            public IDictionary<string, string> Fields { get; set; }
        }
    }
}
