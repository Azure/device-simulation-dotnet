// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class SimulationPatch
    {
        public string ETag { get; set; }
        public string Id { get; set; }
        public bool? Enabled { get; set; }
        public SimulationStatistics Statistics { get; set; }
    }

    public class SimulationStatistics
    {
        public long TotalMessagesSent { get; set; }
        public double AverageMessagesPerSecond { get; set; }
    }
}
