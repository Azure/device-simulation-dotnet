// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic.Models
{
    public enum Status
    {
        None = 0,
        Ready = 1,
        Connecting = 2,
        BootstrappingDevice = 3,
        UpdatingDeviceState = 4,
        UpdatingReportedProperties = 5,
        SendingTelemetry = 6
    }
}
