// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties
{
    public interface IDevicePropertiesLogic
    {
        void Setup(IDevicePropertiesActor devicePropertiesActor, string deviceId, DeviceModel deviceModel);
        void Run();
    }
}