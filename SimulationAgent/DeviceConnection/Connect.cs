// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Establish a connection to Azure IoT Hub
    /// </summary>
    public class Connect : IDeviceConnectionLogic
    {
        private readonly IScriptInterpreter scriptInterpreter;
        private readonly ILogger log;
        private readonly IInstance instance;
        private string deviceId;
        private DeviceModel deviceModel;
        private IDeviceConnectionActor deviceConnectionActor;

        public Connect(
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

            this.log.Debug("Connecting...", () => new { this.deviceId });
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                this.deviceConnectionActor.Client = this.deviceConnectionActor.SimulationContext.Devices.GetClient(
                    this.deviceConnectionActor.Device, 
                    this.deviceModel.Protocol, 
                    this.scriptInterpreter);

                await this.deviceConnectionActor.Client.ConnectAsync();
                await this.deviceConnectionActor.Client.RegisterMethodsForDeviceAsync(
                    this.deviceModel.CloudToDeviceMethods,
                    this.deviceConnectionActor.DeviceState,
                    this.deviceConnectionActor.DeviceProperties);

                await this.deviceConnectionActor.Client.RegisterDesiredPropertiesUpdateAsync(this.deviceConnectionActor.DeviceProperties);

                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Debug("Device connected", () => new { timeSpentMsecs, this.deviceId });
                this.deviceConnectionActor.HandleEvent(DeviceConnectionActor.ActorEvents.Connected);
            }
            catch (DeviceAuthFailedException e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Invalid connection credentials", () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceConnectionActor.HandleEvent(DeviceConnectionActor.ActorEvents.AuthFailed);
            }
            catch (DeviceNotFoundException e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Device not found", () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceConnectionActor.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceNotFound);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Connection error", () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceConnectionActor.HandleEvent(DeviceConnectionActor.ActorEvents.ConnectionFailed);
            }
        }
    }
}
