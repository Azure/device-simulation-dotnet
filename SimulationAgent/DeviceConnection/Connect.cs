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
        private readonly ILogger log;
        private readonly IInstance instance;
        private string deviceId;
        private DeviceModel deviceModel;
        private IDeviceConnectionActor deviceContext;
        private ISimulationContext simulationContext;

        public Connect(
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
            this.simulationContext = context.SimulationContext;
            this.deviceId = deviceId;
            this.deviceModel = deviceModel;

            this.instance.InitComplete();
        }

        public async Task RunAsync()
        {
            this.instance.InitRequired();

            this.log.Debug("Connecting...", () => new { this.deviceId });

            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            try
            {
                // Ensure pending task are stopped
                this.deviceContext.DisposeClient();

                this.deviceContext.Client = this.simulationContext.Devices.GetClient(
                    this.deviceContext.Device,
                    this.deviceModel.Protocol);

                await this.deviceContext.Client.ConnectAsync();
                await this.deviceContext.Client.RegisterMethodsForDeviceAsync(
                    this.deviceModel.CloudToDeviceMethods,
                    this.deviceContext.DeviceState,
                    this.deviceContext.DeviceProperties,
                    this.deviceContext.ScriptInterpreter);

                await this.deviceContext.Client.RegisterDesiredPropertiesUpdateAsync(this.deviceContext.DeviceProperties);

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Debug("Device connected",
                    () => new { timeSpentMsecs, this.deviceId });
                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.Connected);
            }
            catch (DeviceAuthFailedException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Invalid connection credentials",
                    () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.AuthFailed);
            }
            catch (DeviceNotFoundException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Device not found",
                    () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceNotFound);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Connection error",
                    () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.ConnectionFailed);
            }
        }
    }
}
