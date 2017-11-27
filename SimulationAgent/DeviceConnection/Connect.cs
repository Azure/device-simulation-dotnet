﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Establish a connection to Azure IoT Hub
    /// </summary>
    public class Connect : IDeviceConnectionLogic
    {
        private readonly IDevices devices;
        private readonly ILogger log;
        private string deviceId;
        private IoTHubProtocol protocol;
        private IDeviceConnectionActor context;

        public Connect(
            IDevices devices,
            ILogger logger)
        {
            this.log = logger;
            this.devices = devices;
        }

        public void Setup(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel)
        {
            this.context = context;
            this.deviceId = deviceId;
            this.protocol = deviceModel.Protocol;
        }

        public void Run()
        {
            this.log.Debug("Connecting...", () => new { this.deviceId });

            this.context.Client = this.devices.GetClient(this.context.Device, this.protocol);
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.context.Client.ConnectAsync()
                .ContinueWith(t =>
                {
                    var timeTaken = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                    this.log.Debug("Device connected", () => new { this.deviceId, timeTaken });
                    this.context.HandleEvent(DeviceConnectionActor.ActorEvents.Connected);
                });
        }
    }
}
