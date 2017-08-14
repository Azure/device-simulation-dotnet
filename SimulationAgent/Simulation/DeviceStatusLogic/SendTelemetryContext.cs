// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    internal class SendTelemetryContext
    {
        public IDeviceActor Self { get; set; }
        public DeviceModel.DeviceModelMessage Message { get; set; }
    }
}
