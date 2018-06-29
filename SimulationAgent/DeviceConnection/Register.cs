// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Register the device in the hub registry
    /// </summary>
    public class Register : IDeviceConnectionLogic
    {
        private readonly IDevices devices;
        private readonly ILogger log;
        private string deviceId;
        private IDeviceConnectionActor context;

        public Register(IDevices devices, ILogger logger)
        {
            this.log = logger;
            this.devices = devices;
        }

        public void Setup(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel)
        {
            this.context = context;
            this.deviceId = deviceId;
        }

        public async Task RunAsync()
        {
            this.log.Debug("Registering device...", () => new { this.deviceId });

            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var device = await this.devices.CreateAsync(this.deviceId);

                var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                this.log.Debug("Device registered", () => new { this.deviceId, timeSpent });

                this.context.Device = device;
                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceRegistered);
            }
            catch (Exception e)
            {
                this.log.Error("Error while registering the device", () => new { this.deviceId, e });
                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.RegistrationFailed);
            }
        }
    }
}
