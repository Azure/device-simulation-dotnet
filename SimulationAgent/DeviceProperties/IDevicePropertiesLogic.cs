// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties
{
    public interface IDevicePropertiesLogic
    {
        void Init(IDevicePropertiesActor devicePropertiesActor, string deviceId, IDevices devices);
        Task RunAsync();
    }
}