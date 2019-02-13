// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Disconnect from Azure IoT Hub
    /// </summary>
    public class Disconnect : IDeviceConnectionLogic
    {
        private readonly ILogger log;
        private readonly IInstance instance;
        private string deviceId;
        private IDeviceConnectionActor deviceContext;

        public Disconnect(
            ILogger logger,
            IInstance instance)
        {
            this.log = logger;
            this.instance = instance;
        }

        public void Init(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel)
        {
            this.instance.InitOnce();

            this.deviceContext = context;
            this.deviceId = deviceId;

            this.instance.InitComplete();
        }

        public async Task RunAsync()
        {
            this.instance.InitRequired();

            this.log.Debug("Disconnecting...", () => new { this.deviceId });

            try
            {
                await this.deviceContext.Client.DisconnectAsync();

                this.log.Debug("Device disconnected", () => new { this.deviceId });
                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.Disconnected);
            }
            catch (Exception e)
            {
                this.log.Error("Error disconnecting device", () => new { this.deviceId, e });
                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DisconnectionFailed);
            }
        }
    }
}
