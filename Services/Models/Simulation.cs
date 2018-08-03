// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class Simulation
    {
        private DateTimeOffset? startTime;
        private DateTimeOffset? endTime;
        private string iotHubConnectionString;

        [JsonIgnore]
        public string ETag { get; set; }

        [JsonIgnore]
        public string Id { get; set; }

        public bool Enabled { get; set; }
        public IList<DeviceModelRef> DeviceModels { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Modified { get; set; }

        public DateTimeOffset? StartTime
        {
            get => this.startTime;
            set => this.startTime = value ?? DateTimeOffset.MinValue;
        }

        public DateTimeOffset? EndTime
        {
            get => this.endTime;
            set => this.endTime = value ?? DateTimeOffset.MaxValue;
        }

        public string IotHubConnectionString
        {
            get => this.iotHubConnectionString;
            set => this.iotHubConnectionString = value ?? ServicesConfig.USE_DEFAULT_IOTHUB;
        }

        public Simulation()
        {
            // When unspecified, a simulation is enabled
            this.Enabled = true;

            this.StartTime = DateTimeOffset.MinValue;
            this.EndTime = DateTimeOffset.MaxValue;

            // by default, use environment variable
            this.IotHubConnectionString = ServicesConfig.USE_DEFAULT_IOTHUB;
            this.DeviceModels = new List<DeviceModelRef>();
        }

        public class DeviceModelRef
        {
            public string Id { get; set; }
            public int Count { get; set; }
            public DeviceModelOverride Override { get; set; }
        }

        public class DeviceModelOverride
        {
            // Optional field, used to customize the scripts which update the device state
            public DeviceModelSimulationOverride Simulation { get; set; }

            // Optional field, the list can be empty
            // When non empty, the content is merged with (overwriting) the scripts in the device model definition
            // If this list is shorter than the original definition, elements in excess are removed
            // i.e. to keep all the original telemetry messages, there must be an entry for each of them
            public IList<DeviceModelTelemetryOverride> Telemetry { get; set; }

            public DeviceModelOverride()
            {
                this.Simulation = null;
                this.Telemetry = null;
            }
        }

        public class DeviceModelSimulationOverride
        {
            // Optional, used to customize the initial state of the device
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, object> InitialState { get; set; }

            // Optional, used to customize the device state update interval
            public TimeSpan? Interval { get; set; }

            // Optional field, the list can be empty
            // When non empty, the content is merged with (overwriting) the scripts in the device model definition
            // If this list is shorter than the original definition, elements in excess are removed
            // i.e. to keep all the original scripts, there must be an entry for each of them
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public IList<DeviceModelSimulationScriptOverride> Scripts { get; set; }

            public DeviceModelSimulationOverride()
            {
                this.InitialState = null;
                this.Interval = null;
                this.Scripts = null;
            }
        }

        public class DeviceModelSimulationScriptOverride
        {
            // Optional, used to change the script used
            public string Type { get; set; }

            // Optional, used to change the script used
            public string Path { get; set; }

            // Optional, used to provide input parameters to the script
            public object Params { get; set; }

            public DeviceModelSimulationScriptOverride()
            {
                this.Type = null;
                this.Path = null;
                this.Params = null;
            }
        }

        public class DeviceModelTelemetryOverride
        {
            // Optional field, used to customize the telemetry interval
            public TimeSpan? Interval { get; set; }

            // Optional field, when null use the template set in the device model definition
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string MessageTemplate { get; set; }

            // Optional field, when null use the schema set in the device model definition
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public DeviceModelTelemetryMessageSchemaOverride MessageSchema { get; set; }

            public DeviceModelTelemetryOverride()
            {
                this.Interval = null;
                this.MessageTemplate = null;
                this.MessageSchema = null;
            }
        }

        public class DeviceModelTelemetryMessageSchemaOverride
        {
            // Optional, used to customize the name of the message schema
            public string Name { get; set; }

            // Optional, used to change the message format, e.g. from JSON to base64
            public DeviceModel.DeviceModelMessageSchemaFormat? Format { get; set; }

            // Optional, used to replace the list of fields in the schema (the content is not merged)
            public IDictionary<string, DeviceModel.DeviceModelMessageSchemaType> Fields { get; set; }

            public DeviceModelTelemetryMessageSchemaOverride()
            {
                this.Name = null;
                this.Format = null;
                this.Fields = null;
            }
        }

        public bool ShouldBeRunning()
        {
            var now = DateTimeOffset.UtcNow;

            return this.Enabled
                   && (!this.StartTime.HasValue || this.StartTime.Value.CompareTo(now) <= 0)
                   && (!this.EndTime.HasValue || this.EndTime.Value.CompareTo(now) > 0);
        }
    }
}
