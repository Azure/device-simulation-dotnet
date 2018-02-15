// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTwinActor
{
    public interface IDeviceTwinLogic
    {
        void Setup(IDeviceTwinActor deviceTwinActor, string deviceId, Services.Models.DeviceTwin deviceTwin);
        void Run();
    }
}