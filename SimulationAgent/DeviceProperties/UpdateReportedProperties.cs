// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties
{
    public class UpdateReportedProperties : IDevicePropertiesLogic
    {
        private readonly ILogger log;

        private string deviceId;

        private IDevicePropertiesActor context;

        public UpdateReportedProperties(
            IDevices devices,
            IServicesConfig config,
            ILogger logger)
        {
            this.log = logger;
        }

        public void Setup(IDevicePropertiesActor deviceTwinActor, string deviceId, DeviceModel deviceModel)
        {
            // TODO see https://github.com/Azure/device-simulation-dotnet/tree/send-twin-updates
            // for future PR
            throw new NotImplementedException();
        }

        public void Run()
        {
            // TODO see https://github.com/Azure/device-simulation-dotnet/tree/send-twin-updates
            // for future PR
            throw new NotImplementedException();
        }
    }
}
