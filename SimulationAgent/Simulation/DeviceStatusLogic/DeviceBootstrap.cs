// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    public class DeviceBootstrap : IDeviceStatusLogic
    {
        // When connecting to IoT Hub, timeout after 10 seconds
        private static readonly TimeSpan connectionTimeout = TimeSpan.FromSeconds(10);

        private readonly ILogger log;
        private readonly IDevices devices;
        private string deviceId;
        private DeviceType deviceType;

        // Ensure that setup is called once and only once (which helps also detecting thread safety issues)
        private bool setupDone = false;

        public DeviceBootstrap(
            IDevices devices,
            ILogger logger)
        {
            this.devices = devices;
            this.log = logger;
        }

        public void Setup(string deviceId, DeviceType deviceType)
        {
            if (this.setupDone)
            {
                this.log.Error("Setup has already been invoked, are you sharing this instance with multiple devices?",
                    () => new { this.deviceId });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.setupDone = true;
            this.deviceId = deviceId;
            this.deviceType = deviceType;
        }

        public void Run(object context)
        {
            this.ValidateSetup();

            try
            {
                var actor = (IDeviceActor) context;
                if (actor.CancellationToken.IsCancellationRequested)
                {
                    actor.MoveNext();
                    return;
                }

                var device = this.GetDevice(actor.CancellationToken);
                if (IsTwinNotUpdated(device))
                {
                    this.UpdateTwin(device, actor.BootstrapClient, actor.CancellationToken);
                }

                actor.MoveNext();
            }
            catch (Exception e)
            {
                this.log.Error("Error while writing the reported properties",
                    () => new { this.deviceId, e });
            }
        }

        private void UpdateTwin(DeviceServiceModel device, IDeviceClient client, CancellationToken token)
        {
            device.SetReportedProperty("Protocol", this.deviceType.Protocol.ToString());
            device.SetReportedProperty("SupportedMethods", string.Join(",", this.deviceType.CloudToDeviceMethods.Keys));
            device.SetReportedProperty("DeviceType", this.deviceType.GetDeviceTypeReportedProperty());
            device.SetReportedProperty("Telemetry", this.deviceType.GetTelemetryReportedProperty(this.log));
            device.SetReportedProperty("Location", this.deviceType.GetLocationReportedProperty());

            client.UpdateTwinAsync(device).Wait((int) connectionTimeout.TotalMilliseconds, token);

            this.log.Debug("Simulated device properties updated", () => { });
        }

        private static bool IsTwinNotUpdated(DeviceServiceModel device)
        {
            return !device.Twin.ReportedProperties.ContainsKey("Protocol")
                   || !device.Twin.ReportedProperties.ContainsKey("SupportedMethods")
                   || !device.Twin.ReportedProperties.ContainsKey("DeviceType")
                   || !device.Twin.ReportedProperties.ContainsKey("Telemetry");
        }

        private DeviceServiceModel GetDevice(CancellationToken token)
        {
            var task = this.devices.GetAsync(this.deviceId);
            task.Wait((int) connectionTimeout.TotalMilliseconds, token);
            return task.Result;
        }

        private void ValidateSetup()
        {
            if (!this.setupDone)
            {
                this.log.Error("Application error: Setup() must be invoked before Run().",
                    () => new { this.deviceId });
                throw new DeviceActorAlreadyInitializedException();
            }
        }
    }
}
