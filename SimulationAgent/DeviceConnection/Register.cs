// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Register the device in the hub registry
    /// </summary>
    public class Register : IDeviceConnectionLogic
    {
        private readonly ILogger log;
        private IDeviceConnectionActor deviceContext;
        private ISimulationContext simulationContext;
        private string deviceId;

        public Register(ILogger logger)
        {
            this.log = logger;
        }

        public void Init(IDeviceConnectionActor deviceContext, string deviceId, DeviceModel deviceModel)
        {
            this.deviceContext = deviceContext;
            this.simulationContext = deviceContext.SimulationContext;
            this.deviceId = deviceId;
        }

        public async Task RunAsync()
        {
            this.log.Debug("Registering device...", () => new { this.deviceId });
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                var device = await this.simulationContext.Devices.CreateAsync(this.deviceId);

                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Debug("Device registered", () => new { timeSpentMsecs, this.deviceId });

                this.deviceContext.Device = device;
                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceRegistered);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Error while registering the device", () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.RegistrationFailed);
            }
        }
    }
}
