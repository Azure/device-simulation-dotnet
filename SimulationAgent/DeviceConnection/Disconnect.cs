// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
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
        private readonly IDevices devices;
        private readonly IScriptInterpreter scriptInterpreter;
        private readonly ILogger log;
        private string deviceId;
        private DeviceModel deviceModel;
        private IDeviceConnectionActor context;

        public Disconnect(
            IDevices devices,
            IScriptInterpreter scriptInterpreter,
            ILogger logger)
        {
            this.log = logger;
            this.scriptInterpreter = scriptInterpreter;
            this.devices = devices;
        }

        public async Task SetupAsync(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel)
        {
            this.context = context;
            this.deviceId = deviceId;
            this.deviceModel = deviceModel;

            // TODO: to be removed once SimulationContext is introduced
            await this.devices.InitAsync();
        }

        public async Task RunAsync()
        {
            this.log.Debug("Disconnecting...", () => new { this.deviceId });

            try
            {
                this.context.Client = this.devices.GetClient(this.context.Device, this.deviceModel.Protocol, this.scriptInterpreter);

                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await this.context.Client.DisconnectAsync();

                var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                this.log.Debug("Device disconnected", () => new { this.deviceId, timeSpent });
                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.Disconnected);
            }
            catch (Exception e)
            {
                this.log.Error("Error disconnecting device", () => new { this.deviceId, e });
                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.DisconnectionFailed);
            }
        }
    }
}
