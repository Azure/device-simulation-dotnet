// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    public interface IDeviceConnectionLogic
    {
        void Init(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel);
        Task RunAsync();
    }
}
