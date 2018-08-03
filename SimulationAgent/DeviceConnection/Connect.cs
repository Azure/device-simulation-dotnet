// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
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
        private readonly IDevices devices;
        private readonly IScriptInterpreter scriptInterpreter;
        private readonly ILogger log;
        private string deviceId;
        private DeviceModel deviceModel;
        private IDeviceConnectionActor context;

        public Connect(
            IDevices devices,
            IScriptInterpreter scriptInterpreter,
            ILogger logger)
        {
            this.log = logger;
            this.scriptInterpreter = scriptInterpreter;
            this.devices = devices;
        }

        public void Setup(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel)
        {
            this.context = context;
            this.deviceId = deviceId;
            this.deviceModel = deviceModel;
        }

        public async Task RunAsync()
        {
            this.log.Debug("Connecting...", () => new { this.deviceId });
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                this.context.Client = this.devices.GetClient(this.context.Device, this.deviceModel.Protocol, this.scriptInterpreter);

                await this.context.Client.ConnectAsync();
                await this.context.Client.RegisterMethodsForDeviceAsync(this.deviceModel.CloudToDeviceMethods, this.context.DeviceState, this.context.DeviceProperties);
                await this.context.Client.RegisterDesiredPropertiesUpdateAsync(this.context.DeviceProperties);

                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Debug("Device connected", () => new { timeSpentMsecs, this.deviceId });
                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.Connected);
            }
            catch (DeviceAuthFailedException e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Invalid connection credentials", () => new { timeSpentMsecs, this.deviceId, e });
                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.AuthFailed);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Connection error", () => new { timeSpentMsecs, this.deviceId, e });
                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.ConnectionFailed);
            }
        }
    }
}
