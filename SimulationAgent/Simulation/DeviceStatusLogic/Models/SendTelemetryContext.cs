// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic.Models
{
    internal class SendTelemetryContext
    {
        public IDeviceActor DeviceActor { get; set; }
        public DeviceModel.DeviceModelMessage Message { get; set; }
        public ITimer MessageTimer { get; set; }
        public TimeSpan Interval { get; set; }
    }
}
