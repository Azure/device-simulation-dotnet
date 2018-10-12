// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Deregister the device from the hub registry
    /// </summary>
    public class Deregister : IDeviceConnectionLogic
    {
        private readonly IDevices devices;
        private readonly ILogger log;
        private string deviceId;
        private IDeviceConnectionActor context;

        public Deregister(IDevices devices, ILogger logger)
        {
            this.log = logger;
            this.devices = devices;
        }

        public async Task SetupAsync(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel)
        {
            this.context = context;
            this.deviceId = deviceId;

            // TODO: to be removed once SimulationContext is introduced
            await this.devices.InitAsync();
        }

        public async Task RunAsync()
        {
            this.log.Debug("Deregistering device...", () => new { this.deviceId });

            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await this.devices.DeleteAsync(this.deviceId);

                var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                this.log.Debug("Device deregistered", () => new { this.deviceId, timeSpent });

                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceDeregistered);
            }
            catch (Exception e)
            {
                this.log.Error("Error while registering the device", () => new { this.deviceId, e });
                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.DeregisterationFailed);
            }
        }
    }
}
