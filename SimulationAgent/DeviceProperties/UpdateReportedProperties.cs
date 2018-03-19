// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties
{
    public class UpdateReportedProperties : IDevicePropertiesLogic
    {
        private readonly ILogger log;

        private IDevicePropertiesActor context;

        public UpdateReportedProperties(ILogger logger)
        {
            this.log = logger;
        }

        public void Setup(IDevicePropertiesActor devicePropertiesActor, string deviceId)
        {
            // TODO branch for twin updates to IoT Hub located at:
            //      https://github.com/Azure/device-simulation-dotnet/tree/send-twin-updates
        }

        public void Run()
        {
            // TODO branch for twin updates to IoT Hub located at:
            //      https://github.com/Azure/device-simulation-dotnet/tree/send-twin-updates
        }
    }
}
