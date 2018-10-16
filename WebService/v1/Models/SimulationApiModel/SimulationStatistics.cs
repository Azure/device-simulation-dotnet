// Copyright (c) Microsoft. All rights reserved.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class SimulationStatistics
    {
        // Total number of messgaes sent by simulation
        [JsonProperty(PropertyName = "TotalMessagesSent")]
        public long TotalMessagesSent { get; set; }

        // Current message throughput (average messages sent per second) of simulation
        [JsonProperty(PropertyName = "AverageMessagesPerSecond")]
        public double AverageMessagesPerSecond { get; set; }

        // Total number of messages that failed to send
        [JsonProperty(PropertyName = "FailedMessagesCount")]
        public long FailedMessagesCount { get; set; }

        // Total number of devices that are currently connected (i.e. are active) to Hub
        [JsonProperty(PropertyName = "ActiveDevicesCount")]
        public long ActiveDevicesCount { get; set; }

        // Total number of devices that failed to connect to Hub
        [JsonProperty(PropertyName = "FailedDeviceConnectionsCount")]
        public long FailedDeviceConnectionsCount { get; set; }

        // Total number of times device twin failed to update
        [JsonProperty(PropertyName = "FailedDeviceTwinUpdatesCount")]
        public long FailedDeviceTwinUpdatesCount { get; set; }

        // Total number of simulation errors
        [JsonProperty(PropertyName = "SimulationErrorsCount")]
        public long SimulationErrorsCount { get; set; }

        // Default constructor used by web service requests
        public SimulationStatistics()
        {
            this.TotalMessagesSent = 0;
            this.AverageMessagesPerSecond = 0;
            this.FailedMessagesCount = 0;
            this.FailedDeviceConnectionsCount = 0;
            this.FailedDeviceTwinUpdatesCount = 0;
            this.SimulationErrorsCount = 0;
        }

        // Map API model to service model
        public static SimulationStatistics FromServiceModel(Services.Models.SimulationStatisticsModel statistics)
        {
            if (statistics == null) return null;

            return new SimulationStatistics
            {
                TotalMessagesSent = statistics.TotalMessagesSent,
                FailedDeviceConnectionsCount = statistics.FailedDeviceConnectionsCount,
                FailedMessagesCount = statistics.FailedMessagesCount,
                SimulationErrorsCount = statistics.SimulationErrorsCount,
                FailedDeviceTwinUpdatesCount = statistics.FailedDeviceTwinUpdatesCount,
            };
        }
    }
}
