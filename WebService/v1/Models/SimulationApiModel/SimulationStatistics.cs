// Copyright (c) Microsoft. All rights reserved.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class SimulationStatistics
    {
        [JsonProperty(PropertyName = "TotalMessagesSent")]
        public long TotalMessagesSent { get; set; }

        [JsonProperty(PropertyName = "AverageMessagesPerSecond")]
        public double AverageMessagesPerSecond { get; set; }

        [JsonProperty(PropertyName = "FailedMessagesCount")]
        public long FailedMessagesCount { get; set; }

        [JsonProperty(PropertyName = "ActiveDevicesCount")]
        public long ActiveDevicesCount { get; set; }

        [JsonProperty(PropertyName = "FailedDeviceConnectionsCount")]
        public long FailedDeviceConnectionsCount { get; set; }

        [JsonProperty(PropertyName = "FailedDeviceTwinUpdatesCount")]
        public long FailedDeviceTwinUpdatesCount { get; set; }

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
        public static Services.Models.Simulation.StatisticsRef ToServiceModel(SimulationStatistics statistics)
        {
            if(statistics != null)
            {
                return new Services.Models.Simulation.StatisticsRef {
                    TotalMessagesSent = statistics.TotalMessagesSent,
                    AverageMessagesPerSecond = statistics.AverageMessagesPerSecond
                };
            }

            return null;
        }

        // Map API model to service model
        public static SimulationStatistics FromServiceModel(Services.Models.Simulation.StatisticsRef statistics)
        {
            if (statistics != null)
            {
                return new SimulationStatistics
                {
                    TotalMessagesSent = statistics.TotalMessagesSent,
                    AverageMessagesPerSecond = Math.Round(statistics.AverageMessagesPerSecond, 2)
                };
            }

            return null;
        }
    }
}
