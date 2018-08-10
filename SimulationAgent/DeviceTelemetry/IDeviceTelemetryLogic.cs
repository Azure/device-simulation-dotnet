// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry
{
    public interface IDeviceTelemetryLogic
    {
        void Setup(IDeviceTelemetryActor deviceTelemetryActor, string deviceId, DeviceModel deviceModel);
        Task RunAsync();
    }
}