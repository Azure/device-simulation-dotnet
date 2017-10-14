// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    public class DeviceBootstrap : IDeviceStatusLogic
    {
        // Retry frequency when failing to bootstrap (methods registration excluded)
        // The actual frequency is calculated considering the number of methods
        private static readonly TimeSpan retryFrequency = TimeSpan.FromSeconds(60);

        // Device method registration timeout
        private const int METHOD_REGISTRATION_TIMEOUT_SECS = 10;

        // When connecting to IoT Hub, timeout after 10 seconds
        private static readonly TimeSpan connectionTimeout = TimeSpan.FromSeconds(10);

        // Twin write timeout
        private static readonly TimeSpan updateTwinTimeout = TimeSpan.FromSeconds(10);

        private readonly ILogger log;
        private readonly IDevices devices;
        private string deviceId;
        private DeviceModel deviceModel;

        // The timer invoking the Run method
        private readonly ITimer timer;

        // Ensure that setup is called once and only once (which helps also detecting thread safety issues)
        private bool setupDone = false;

        private IDeviceActor context;

        public DeviceBootstrap(
            ITimer timer,
            IDevices devices,
            ILogger logger)
        {
            this.timer = timer;
            this.devices = devices;
            this.log = logger;
        }

        public void Setup(string deviceId, DeviceModel deviceModel, IDeviceActor context)
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

            this.context = context;

            // Calculate the timeout considering the number of methods to register
            var retryPeriod = TimeSpan.FromSeconds(
                retryFrequency.Seconds
                + METHOD_REGISTRATION_TIMEOUT_SECS * this.deviceModel.CloudToDeviceMethods.Count);

            this.timer.Setup(this.Run, retryPeriod);
        }

        public void Start()
        {
            this.log.Info("Starting DeviceBootstrap timer",
                () => new { this.context.DeviceId });
            this.timer.Start();
        }

        public void Stop()
        {
            this.log.Info("Starting DeviceBootstrap timer",
                () => new { this.context.DeviceId });
            this.timer.Stop();
        }

        public void Run(object context)
        {
            try
            {
                this.timer.Pause();
                this.RunInternal();
            }
            finally
            {
                this.timer.Resume();
            }
        }

        private void RunInternal()
        {
            this.ValidateSetup();

            try
            {
                var actor = this.context;
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
                actor.BootstrapClient.RegisterMethodsForDevice(this.deviceModel.CloudToDeviceMethods, actor.DeviceState);
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

            client.UpdateTwinAsync(device).Wait((int) updateTwinTimeout.TotalMilliseconds, token);

            this.log.Debug("Simulated device properties updated", () => { });
        }

        // TODO: we should set this on creation, so we save one Read and one Write operation
        private static bool IsTwinNotUpdated(Device device)
        {
            return !device.Twin.ReportedProperties.ContainsKey("Protocol")
                   || !device.Twin.ReportedProperties.ContainsKey("SupportedMethods")
                   || !device.Twin.ReportedProperties.ContainsKey("Telemetry");
        }

        private Device GetDevice(CancellationToken token)
        {
            var task = this.devices.GetAsync(this.deviceId, true);
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
