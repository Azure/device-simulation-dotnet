// Copyright (c) Microsoft. All rights reserved.

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
        public long FailedMessages { get; set; }

        // Total number of devices that failed to connect to Hub
        [JsonProperty(PropertyName = "FailedDeviceConnectionsCount")]
        public long FailedDeviceConnections { get; set; }

        // Total number of times device twin failed to update
        [JsonProperty(PropertyName = "FailedDeviceTwinUpdatesCount")]
        public long FailedDevicePropertiesUpdates { get; set; }

        // Total number of devices that are currently connected (i.e. are active) to Hub
        [JsonProperty(PropertyName = "ActiveDevicesCount")]
        public long ActiveDevices { get; set; }

        // Default constructor used by web service requests
        public SimulationStatistics()
        {
            this.ActiveDevices = 0;
            this.TotalMessagesSent = 0;
            this.AverageMessagesPerSecond = 0;
            this.FailedMessages = 0;
            this.FailedDeviceConnections = 0;
            this.FailedDevicePropertiesUpdates = 0;
        }

        // Map API model to service model
        public static SimulationStatistics FromServiceModel(Services.Models.SimulationStatisticsModel statistics)
        {
            if (statistics == null) return null;

            return new SimulationStatistics
            {
                ActiveDevices = statistics.ActiveDevices,
                TotalMessagesSent = statistics.TotalMessagesSent,
                FailedDeviceConnections = statistics.FailedDeviceConnections,
                FailedMessages = statistics.FailedMessages,
                FailedDevicePropertiesUpdates = statistics.FailedDevicePropertiesUpdates,
            };
        }
    }
}
