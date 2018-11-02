// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class SimulationStatisticsModel
    {
        public long ActiveDevices { get; set; }
        public long TotalMessagesSent { get; set; }
        public long FailedMessages { get; set; }
        public long FailedDeviceConnections { get; set; }
        public long FailedDevicePropertiesUpdates { get; set; }
    }
}
