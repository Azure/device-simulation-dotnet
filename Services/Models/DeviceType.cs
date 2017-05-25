// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class DeviceType
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public IoTHubProtocol Protocol { get; set; }
        public IDictionary<string, DeviceTypeFunction> Functions { get; set; }
        public DeviceTypeTelemetry Telemetry { get; set; }
        public IDictionary<string, DeviceTypeMethod> Methods { get; set; }

        public DeviceType()
        {
            this.Functions = new Dictionary<string, DeviceTypeFunction>();
            this.Methods = new Dictionary<string, DeviceTypeMethod>();
        }

        public class DeviceTypeFunction
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Path { get; set; }
        }

        public class DeviceTypeMethod
        {
            public DeviceTypeMethod()
            {
                this.Actions = new List<string>();
            }

            public string Name { get; set; }
            public IList<string> Actions { get; set; }
        }

        public class DeviceTypeTelemetry
        {
            public DeviceTypeTelemetry()
            {
                this.Messages = new List<DeviceTypeMessage>();
            }

            public IList<DeviceTypeMessage> Messages { get; set; }
        }

        public class DeviceTypeMessage
        {
            public TimeSpan Interval { get; set; }
            public string Message { get; set; }
            public DeviceTypeMessageSchema MessageSchema { get; set; }
        }

        public class DeviceTypeMessageSchema
        {
            public DeviceTypeMessageSchema()
            {
                this.Fields = new Dictionary<string, DeviceTypeMessageSchemaType>();
            }

            public string Name { get; set; }
            public DeviceTypeMessageSchemaFormat Format { get; set; }
            public IDictionary<string, DeviceTypeMessageSchemaType> Fields { get; set; }
        }

        public enum DeviceTypeMessageSchemaFormat
        {
            JSON = 1
        }

        public enum DeviceTypeMessageSchemaType
        {
            Object = 0,
            Binary = 10,
            Text = 30,
            Boolean = 20,
            Integer = 40,
            Double = 50,
            DateTime = 60
        }
    }
}
