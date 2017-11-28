// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Fetch the device from the registry if available
    /// </summary>
    public class Fetch : IDeviceConnectionLogic
    {
        private readonly IDevices devices;
        private readonly ILogger log;
        private string deviceId;
        private IDeviceConnectionActor context;

        public Fetch(IDevices devices, ILogger logger)
        {
            this.log = logger;
            this.devices = devices;
        }

        public void Setup(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel)
        {
            this.context = context;
            this.deviceId = deviceId;
        }

        public void Run()
        {
            this.log.Debug("Fetching device...", () => new { this.deviceId });

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.devices.GetAsync(this.deviceId)
                .ContinueWith(t =>
                {
                    var timeTaken = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                    var device = t.Result;
                    if (device != null)
                    {
                        this.context.Device = device;
                        this.log.Debug("Device found", () => new { device.Id, timeTaken, device.Enabled });
                        this.context.HandleEvent(DeviceConnectionActor.ActorEvents.FetchCompleted);
                    }
                    else
                    {
                        this.log.Debug("Device not found", () => new { this.deviceId, timeTaken });
                        this.context.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceNotFound);
                    }
                });
        }
    }
}
