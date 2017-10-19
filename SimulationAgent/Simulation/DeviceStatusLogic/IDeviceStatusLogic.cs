// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    public interface IDeviceStatusLogic
    {
        void Setup(string deviceId, DeviceModel deviceModel, IDeviceActor context);
        void Start();
        void Stop();
        void Run(object context);
    }
}
