// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Newtonsoft.Json.Linq;

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
            this.CloudToDeviceMethods = new Dictionary<string, Script>();
        }

        /// <summary>
        /// If the simulated device has some geolocation properties, we publish
        /// them also in the twin, so that the device can be shown in the map
        /// even when it hasn't sent (or is not sending) telemetry.
        /// </summary>
        public JObject GetLocationReportedProperty()
        {
            // 3D
            if (this.DeviceState.Initial.ContainsKey("latitude")
                && this.DeviceState.Initial.ContainsKey("longitude")
                && this.DeviceState.Initial.ContainsKey("altitude"))
            {
                return new JObject
                {
                    ["Latitude"] = this.DeviceState.Initial["latitude"].ToString(),
                    ["Longitude"] = this.DeviceState.Initial["longitude"].ToString(),
                    ["Altitude"] = this.DeviceState.Initial["altitude"].ToString()
                };
            }

            // 2D
            if (this.DeviceState.Initial.ContainsKey("latitude")
                && this.DeviceState.Initial.ContainsKey("longitude"))
            {
                return new JObject
                {
                    ["Latitude"] = this.DeviceState.Initial["latitude"].ToString(),
                    ["Longitude"] = this.DeviceState.Initial["longitude"].ToString()
                };
            }

            // Geostationary
            if (this.DeviceState.Initial.ContainsKey("longitude"))
            {
                return new JObject
                {
                    ["Longitude"] = this.DeviceState.Initial["longitude"].ToString()
                };
            }

            return null;
        }

        /// <summary>
        /// This data is published in the device twin, so that the UI can
        /// show information about a device.
        /// </summary>
        public JObject GetDeviceTypeReportedProperty()
        {
            return new JObject
            {
                ["Name"] = this.Name,
                ["Version"] = this.Version
            };
        }

        /// <summary>
        /// This data is published in the device twin, so that clients
        /// parsing the telemetry have information about the schema used,
        /// e.g. whether the format is JSON or something else, the list of
        /// fields and their type.
        /// </summary>
        public JObject GetTelemetryReportedProperty(ILogger log)
        {
            var result = new JObject();

            foreach (var t in this.Telemetry)
            {
                if (t == null)
                {
                    log.Error("The device type contains an invalid message definition",
                        () => new { this.Id, this.Name });
                    throw new InvalidConfigurationException("The device type contains an invalid message definition");
                }

                if (string.IsNullOrEmpty(t.MessageSchema.Name))
                {
                    log.Error("One of the device messages schema doesn't have a name specified",
                        () => new { this.Id, this.Name, t });
                    throw new InvalidConfigurationException("One of the device messages schema doesn't have a name specified");
                }

                var fields = new JObject();
                foreach (var field in t.MessageSchema.Fields)
                {
                    fields[field.Key] = field.Value.ToString();
                }

                var schema = new JObject
                {
                    ["Name"] = t.MessageSchema.Name,
                    ["Format"] = t.MessageSchema.Format.ToString(),
                    ["Fields"] = fields
                };

                var message = new JObject
                {
                    ["Interval"] = t.Interval,
                    ["MessageTemplate"] = t.MessageTemplate,
                    ["MessageSchema"] = schema
                };

                result[t.MessageSchema.Name] = message;
            }

            return result;
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
