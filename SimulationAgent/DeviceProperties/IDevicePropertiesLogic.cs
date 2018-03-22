// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties
{
    public interface IDevicePropertiesLogic
    {
        void Setup(IDevicePropertiesActor devicePropertiesActor, string deviceId);
        void Run();
    }
}