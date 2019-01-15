// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Disconnect from Azure IoT Hub
    /// </summary>
    public class Disconnect : IDeviceConnectionLogic
    {
        private readonly ILogger log;

        public Disconnect(ILogger logger)
        {
            this.log = logger;
        }

        public async Task RunAsync(IDeviceConnectionActor deviceContext)
        {
            var deviceId = deviceContext.DeviceId;

            try
            {
                this.log.Debug("Disconnecting...", () => new { deviceId });

                await deviceContext.Client.DisconnectAsync();

                this.log.Debug("Device disconnected", () => new { deviceId });
                deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.Disconnected);
            }
            catch (Exception e)
            {
                this.log.Error("Error disconnecting device", () => new { deviceId, e });
                deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DisconnectionFailed);
            }
        }
    }
}
