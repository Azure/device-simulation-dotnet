// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{

    public class Script
    {
        public string Type { get; set; }
        public string Path { get; set; }

        public Script()
        {
            this.Type = "javascript";
            this.Path = "scripts" + System.IO.Path.DirectorySeparatorChar;
        }
    }
    
    public class DeviceType
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public IoTHubProtocol Protocol { get; set; }
        public InternalState DeviceState { get; set; }
        public IList<DeviceTypeMessage> Telemetry { get; set; }
        public IDictionary<string, Script> CloudToDeviceMethods { get; set; }

        public DeviceType()
        {
            this.Id = string.Empty;
            this.Version = "0.0.0";
            this.Name = string.Empty;
            this.Description = string.Empty;
            this.Protocol = IoTHubProtocol.AMQP;
            this.DeviceState = new InternalState();
            this.Telemetry = new List<DeviceTypeMessage>();
        }

        public class InternalState
        {
            public Dictionary<string, object> Initial { get; set; }
            public TimeSpan SimulationInterval { get; set; }
            public Script SimulationScript { get; set; }

            public InternalState()
            {
                this.Initial = new Dictionary<string, object>();
                this.SimulationInterval = TimeSpan.Zero;
                this.SimulationScript = new Script();
            }
        }

        public class DeviceTypeMessage
        {
            public TimeSpan Interval { get; set; }
            public string MessageTemplate { get; set; }
            public DeviceTypeMessageSchema MessageSchema { get; set; }

            public DeviceTypeMessage()
            {
                this.Interval = TimeSpan.Zero;
                this.MessageTemplate = string.Empty;
                this.MessageSchema = new DeviceTypeMessageSchema();
            }
        }

        public class DeviceTypeMessageSchema
        {
            public string Name { get; set; }
            public DeviceTypeMessageSchemaFormat Format { get; set; }
            public IDictionary<string, DeviceTypeMessageSchemaType> Fields { get; set; }

            public DeviceTypeMessageSchema()
            {
                this.Name = string.Empty;
                this.Format = DeviceTypeMessageSchemaFormat.JSON;
                this.Fields = new Dictionary<string, DeviceTypeMessageSchemaType>();
            }
        }

        public enum DeviceTypeMessageSchemaFormat
        {
            Binary = 0,
            Text = 10,
            JSON = 20
        }

        public enum DeviceTypeMessageSchemaType
        {
            Object = 0,
            Binary = 10,
            Text = 20,
            Boolean = 30,
            Integer = 40,
            Double = 50,
            DateTime = 60
        }
    }
}
