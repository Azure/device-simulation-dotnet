// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTwinActor;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTwin
{
    /// <summary>
    /// Logic executed after Connect() succeeds, to send device twin updates
    /// to the IoT Hub
    /// </summary>
    public class UpdateTwin : IDeviceTwinLogic
    {
        private readonly ILogger log;

        private string deviceId;

        private IDeviceTwinActor context;
        private Services.Models.DeviceTwin deviceTwin;

        public UpdateTwin(
            ILogger logger)
        {
            this.log = logger;
        }

        public void Setup(IDeviceTwinActor context, string deviceId, Services.Models.DeviceTwin deviceTwin)
        {
            this.context = context;
            this.deviceId = deviceId;
            this.deviceTwin = deviceTwin;
        }

        public void Run()
        {
            // TODO
        }
    }
}