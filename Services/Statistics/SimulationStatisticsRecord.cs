// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Statistics
{
    public class SimulationStatisticsRecord
    {
        public string SimulationId { get; set; }
        public string NodeId { get; set; }
        public SimulationStatisticsModel Statistics { get; set; }
    }
}
