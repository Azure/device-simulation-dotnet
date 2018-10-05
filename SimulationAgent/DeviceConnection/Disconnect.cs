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
        private IDeviceConnectionActor deviceConnectionActor;

        public Disconnect(
            IScriptInterpreter scriptInterpreter,
            ILogger logger,
            IInstance instance)
        {
            this.scriptInterpreter = scriptInterpreter;
            this.log = logger;
            this.instance = instance;
        }

        public void Init(IDeviceConnectionActor actor, string deviceId, DeviceModel deviceModel)
        {
            this.instance.InitOnce();

            this.deviceConnectionActor = actor;
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
                this.deviceConnectionActor.Client = this.deviceConnectionActor.SimulationContext.Devices.GetClient(
                    this.deviceConnectionActor.Device, 
                    this.deviceModel.Protocol, 
                    this.scriptInterpreter);

                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await this.deviceConnectionActor.Client.DisconnectAsync();

                var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                this.log.Debug("Device disconnected", () => new { this.deviceId, timeSpent });
                this.deviceConnectionActor.HandleEvent(DeviceConnectionActor.ActorEvents.Disconnected);
            }
            catch (Exception e)
            {
                this.log.Error("Error disconnecting device", () => new { this.deviceId, e });
                this.deviceConnectionActor.HandleEvent(DeviceConnectionActor.ActorEvents.DisconnectionFailed);
            }
        }
    }
}
