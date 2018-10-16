// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class SimulationStatisticsModel
    {
        public long TotalMessagesSent { get; set; }
        public long FailedMessagesCount { get; set; }
        public long FailedDeviceConnectionsCount { get; set; }
        public long FailedDeviceTwinUpdatesCount { get; set; }
        public long SimulationErrorsCount { get; set; }
    }
}
