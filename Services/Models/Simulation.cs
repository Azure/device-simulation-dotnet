// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class Simulation
    {
        private DateTimeOffset? startTime;
        private DateTimeOffset? endTime;
        private IList<string> iotHubConnectionStrings;

        // A simulation is "active" if enabled and "scheduled"
        [JsonIgnore]
        public bool IsActiveNow
        {
            get
            {
                var now = DateTimeOffset.UtcNow;
                var startedInThePast = !this.StartTime.HasValue || this.StartTime.Value.CompareTo(now) <= 0;
                var endInTheFuture = !this.EndTime.HasValue || this.EndTime.Value.CompareTo(now) > 0;
                return this.Enabled && startedInThePast && endInTheFuture;
            }
        }

        [JsonIgnore]
        public bool DeviceCreationRequired => this.IsActiveNow && !this.DevicesCreationComplete;

        [JsonIgnore]
        public bool DeviceDeletionRequired => !this.IsActiveNow
                                              && this.DevicesCreationComplete
                                              && (this.DeleteDevicesWhenSimulationEnds || this.DeleteDevicesOnce);

        [JsonIgnore]
        public bool PartitioningRequired => this.IsActiveNow && !this.PartitioningComplete;

        // A simulation should be running if it is active and devices have been created and partitioned
        [JsonIgnore]
        public bool ShouldBeRunning => this.IsActiveNow
                                       && this.PartitioningComplete
                                       && this.DevicesCreationComplete;

        // When Simulation is written to storage, Id and Etag are not serialized as part of body
        // These are instead written in dedicated columns (key and eTag)
        [JsonIgnore]
        public string ETag { get; set; }

        [JsonIgnore]
        public string Id { get; set; }

        [JsonProperty(Order = 10)]
        public bool Enabled { get; set; }

        [JsonProperty(Order = 11)]
        public bool DevicesCreationStarted { get; set; }

        [JsonProperty(Order = 12)]
        public bool DevicesCreationComplete { get; set; }

        [JsonProperty(Order = 13)]
        public bool DeleteDevicesWhenSimulationEnds { get; set; }

        [JsonProperty(Order = 14)]
        public bool DevicesDeletionStarted { get; set; }

        [JsonProperty(Order = 15)]
        public bool DevicesDeletionComplete { get; set; }

        [JsonProperty(Order = 16)]
        public bool DeleteDevicesOnce { get; set; }

        [JsonProperty(Order = 1000)]
        public string DeviceCreationJobId { get; set; }

        [JsonProperty(Order = 1001)]
        public string DeviceDeletionJobId { get; set; }

        [JsonProperty(Order = 20)]
        public string Name { get; set; }

        [JsonProperty(Order = 30)]
        public string Description { get; set; }

        [JsonProperty(Order = 13)]
        public bool PartitioningComplete { get; set; }

        [JsonProperty(Order = 50)]
        public IList<DeviceModelRef> DeviceModels { get; set; }

        [JsonProperty(Order = 60)]
        public SimulationStatisticsModel Statistics { get; set; }

        [JsonProperty(Order = 70)]
        public IRateLimitingConfig RateLimits { get; set; }

        [JsonProperty(Order = 80)]
        public IList<CustomDeviceRef> CustomDevices { get; set; }

        [JsonProperty(Order = 90)]
        public IList<string> IotHubConnectionStrings
        {
            get => this.iotHubConnectionStrings;
            set => this.iotHubConnectionStrings = value ?? new List<string>();
        }

        // StartTime is the time when Simulation was started
        [JsonProperty(Order = 100)]
        public DateTimeOffset? StartTime
        {
            get => this.startTime;
            set => this.startTime = value ?? DateTimeOffset.MinValue;
        }

        // EndTime is the time when Simulation ended after running for scheduled duration
        [JsonProperty(Order = 110)]
        public DateTimeOffset? EndTime
        {
            get => this.endTime;
            set => this.endTime = value ?? DateTimeOffset.MaxValue;
        }

        // StoppedTime is the time when Simulation was explicitly stopped by user
        [JsonProperty(Order = 120)]
        public DateTimeOffset? StoppedTime { get; set; }

        [JsonProperty(Order = 130)]
        public DateTimeOffset Created { get; set; }

        [JsonProperty(Order = 140)]
        public DateTimeOffset Modified { get; set; }

        // ActualStartTime is the time when Simulation was started
        [JsonProperty(Order = 150)]
        public DateTimeOffset? ActualStartTime { get; set; }

        // ReplayFileId is the replay file data in storage
        [JsonProperty(Order = 160)]
        public string ReplayFileId { get; set; }

        public bool ReplayFileRunIndefinitely { get; set; }

        public Simulation()
        {
            // When unspecified, a simulation is enabled
            this.Enabled = true;

            // By default, a new simulation requires partitioning
            this.PartitioningComplete = false;

            // by default, use environment variable
            this.IotHubConnectionStrings = new List<string>();

            // By default, run forever
            this.StartTime = DateTimeOffset.MinValue;
            this.EndTime = DateTimeOffset.MaxValue;

            this.DeviceModels = new List<DeviceModelRef>();
            this.CustomDevices = new List<CustomDeviceRef>();
            this.RateLimits = new RateLimitingConfig();
        }

        public class DeviceModelRef
        {
            public string Id { get; set; }
            public int Count { get; set; }
            public DeviceModelOverride Override { get; set; }
        }

        public class CustomDeviceRef
        {
            public string DeviceId { get; set; }
            public DeviceModelRef DeviceModel { get; set; }
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
            public IList<DeviceModelScriptOverride> Scripts { get; set; }

            public DeviceModelSimulationOverride()
            {
                this.InitialState = null;
                this.Interval = null;
                this.Scripts = null;
            }
        }

        public class DeviceModelScriptOverride
        {
            // Optional, used to change the script used
            public string Type { get; set; }

            // Optional, used to change the script used
            public string Path { get; set; }

            // Optional, used to change the script used
            public string Id { get; set; }

            // Optional, used to provide input parameters to the script
            public object Params { get; set; }

            public DeviceModelScriptOverride()
            {
                this.Type = null;
                this.Path = null;
                this.Id = null;
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

            public string ClassName { get; set; }

            // Optional, used to change the message format, e.g. from JSON to base64
            public DeviceModel.DeviceModelMessageSchemaFormat? Format { get; set; }

            // Optional, used to replace the list of fields in the schema (the content is not merged)
            public IDictionary<string, DeviceModel.DeviceModelMessageSchemaType> Fields { get; set; }

            public DeviceModelTelemetryMessageSchemaOverride()
            {
                this.Name = null;
                this.ClassName = null;
                this.Format = null;
                this.Fields = null;
            }
        }
    }
}
