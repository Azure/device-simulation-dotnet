// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class Simulation
    {
        private const string USE_LOCAL_IOTHUB = "default";

        private DateTimeOffset? startTime;
        private DateTimeOffset? endTime;
        private string iotHubConnectionString;

        public string ETag { get; set; }
        public string Id { get; set; }
        public bool Enabled { get; set; }
        public IList<DeviceModelRef> DeviceModels { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Modified { get; set; }
        public long Version { get; set; }

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
            set => this.iotHubConnectionString = value ?? USE_LOCAL_IOTHUB;
        }

        public Simulation()
        {
            this.StartTime = DateTimeOffset.MinValue;
            this.EndTime = DateTimeOffset.MaxValue;

            // When unspecified, a simulation is enabled
            this.Enabled = true;
            // by default, use PCS_IOTHUB_CONNSTRING
            this.IotHubConnectionString = USE_LOCAL_IOTHUB;
            this.DeviceModels = new List<DeviceModelRef>();
        }

        public class DeviceModelRef
        {
            public string Id { get; set; }
            public int Count { get; set; }
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
