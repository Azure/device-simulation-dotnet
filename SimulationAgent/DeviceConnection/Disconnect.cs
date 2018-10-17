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
        private readonly IScriptInterpreter scriptInterpreter;
        private readonly ILogger log;
        private readonly IInstance instance;
        private string deviceId;
        private DeviceModel deviceModel;
        private IDeviceConnectionActor deviceContext;
        private ISimulationContext simulationContext;

        public Disconnect(
            IScriptInterpreter scriptInterpreter,
            ILogger logger,
            IInstance instance)
        {
            this.scriptInterpreter = scriptInterpreter;
            this.log = logger;
            this.instance = instance;
        }

        public void Init(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel)
        {
            this.instance.InitOnce();

            this.deviceContext = context;
            this.simulationContext = context.SimulationContext;
            this.deviceId = deviceId;
            this.deviceModel = deviceModel;

            this.instance.InitComplete();
        }

        public async Task RunAsync()
        {
            this.instance.InitRequired();

            this.log.Debug("Disconnecting...", () => new { this.deviceId });

            try
            {
                // TODO: we should already have a client in the device context. If
                //       we call GetClient here, we might be getting a new client 
                //       which will be in a default state.
                this.deviceContext.Client = this.simulationContext.Devices.GetClient(
                    this.deviceContext.Device,
                    this.deviceModel.Protocol,
                    this.scriptInterpreter);

                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await this.deviceContext.Client.DisconnectAsync();

                var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                this.log.Debug("Device disconnected", () => new { this.deviceId, timeSpent });
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
