// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    public class DeviceBootstrap : IDeviceStatusLogic
    {
        // When connecting to IoT Hub, timeout after 10 seconds
        private static readonly TimeSpan connectionTimeout = TimeSpan.FromSeconds(10);

        private readonly ILogger log;
        private readonly IDevices devices;
        private string deviceId;
        private DeviceModel deviceModel;
        private readonly IScriptInterpreter scriptInterpreter;

        // Ensure that setup is called once and only once (which helps also detecting thread safety issues)
        private bool setupDone = false;

        public DeviceBootstrap(
            IDevices devices,
            ILogger logger,
            IScriptInterpreter scriptInterpreter)
        {
            this.devices = devices;
            this.log = logger;
            this.scriptInterpreter = scriptInterpreter;
        }

        public void Setup(string deviceId, DeviceModel deviceModel)
        {
            if (this.setupDone)
            {
                this.log.Error("Setup has already been invoked, are you sharing this instance with multiple devices?",
                    () => new { this.deviceId });
                throw new DeviceActorAlreadyInitializedException();
            }
            this.setupDone = true;

            this.deviceId = deviceId;
            this.deviceModel = deviceModel;
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

                // register methods for the device
                actor.BootstrapClient.RegisterMethodsForDevice(actor.DeviceState, this.deviceModel.CloudToDeviceMethods, this.scriptInterpreter);
                actor.MoveNext();
            }
            catch (Exception e)
            {
                this.log.Error("Error while writing the reported properties",
                    () => new { this.deviceId, e });
            }
        }
                
        private void UpdateTwin(Device device, IDeviceClient client, CancellationToken token)
        {
            // Generate some properties using the device model specs
            device.SetReportedProperty("Protocol", this.deviceModel.Protocol.ToString());
            device.SetReportedProperty("SupportedMethods", string.Join(",", this.deviceModel.CloudToDeviceMethods.Keys));
            device.SetReportedProperty("Telemetry", this.deviceModel.GetTelemetryReportedProperty(this.log));

            // Copy all the properties defined in the device model specs
            foreach (KeyValuePair<string, object> p in this.deviceModel.Properties)
            {
                device.SetReportedProperty(p.Key, new JValue(p.Value));
            }

            client.UpdateTwinAsync(device).Wait((int) connectionTimeout.TotalMilliseconds, token);

            this.log.Debug("Simulated device properties updated", () => { });
        }

        private static bool IsTwinNotUpdated(Device device)
        {
            return !device.Twin.ReportedProperties.ContainsKey("Protocol")
                   || !device.Twin.ReportedProperties.ContainsKey("SupportedMethods")
                   || !device.Twin.ReportedProperties.ContainsKey("Telemetry");
        }

        private Device GetDevice(CancellationToken token)
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
