// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Register the device in IoT registry
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

        public void Run()
        {
            this.log.Debug("Registering device...", () => new { this.deviceId });

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.devices.CreateAsync(this.deviceId)
                .ContinueWith(t =>
                {
                    var timeTaken = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                    var device = t.Result;
                    this.log.Debug("Device created", () => new { this.deviceId, timeTaken });
                    this.context.Device = device;
                    this.context.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceRegistered);
                });
        }
    }
}