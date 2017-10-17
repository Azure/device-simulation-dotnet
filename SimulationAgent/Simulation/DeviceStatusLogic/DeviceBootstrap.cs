// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic.Models;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    public class DeviceBootstrap : IDeviceStatusLogic
    {
        // Retry frequency when failing to bootstrap (methods registration excluded)
        // The actual frequency is calculated considering the number of methods
        private const int RETRY_FREQUENCY_MSECS = 60000;

        // Device method registration timeout
        private const int METHOD_REGISTRATION_TIMEOUT_MSECS = 10000;

        private readonly ILogger log;
        private readonly IDevices devices;
        private string deviceId;
        private DeviceModel deviceModel;

        // The timer invoking the Run method
        private readonly ITimer timer;

        // Ensure that setup is called once and only once (which helps also detecting thread safety issues)
        private bool setupDone = false;

        private IDeviceActor context;
        private int retryPeriodMsecs;

        public DeviceBootstrap(
            ITimer timer,
            IDevices devices,
            ILogger logger)
        {
            this.timer = timer;
            this.devices = devices;
            this.log = logger;
            this.timer.Setup(this.Run);
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
            this.retryPeriodMsecs = RETRY_FREQUENCY_MSECS
                                    + METHOD_REGISTRATION_TIMEOUT_MSECS * this.deviceModel.CloudToDeviceMethods.Count;
        }

        public void Start()
        {
            this.log.Info("Starting DeviceBootstrap", () => new { this.deviceId });
            this.timer.RunOnce(0);
        }

        public void Stop()
        {
            this.log.Info("Stopping DeviceBootstrap", () => new { this.deviceId });
            this.timer.Cancel();
        }

        public void Run(object context)
        {
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            try
            {
                this.RunInternalAsync().Wait();
            }
            finally
            {
                if (this.context.ActorStatus == Status.BootstrappingDevice)
                {
                    var passed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                    this.timer.RunOnce(this.retryPeriodMsecs - passed);
                }
            }
        }

        private async Task RunInternalAsync()
        {
            this.ValidateSetup();

            var actor = this.context;
            if (actor.CancellationToken.IsCancellationRequested)
            {
                actor.MoveNext();
                return;
            }

            if (this.context.ActorStatus == Status.BootstrappingDevice)
            {
                this.log.Debug("Boostrapping...", () => new { this.deviceId });

                try
                {
                    var device = await this.devices.GetAsync(this.deviceId, true, actor.CancellationToken);
                    if (IsTwinNotUpdated(device))
                    {
                        await this.UpdateTwinAsync(device, actor.BootstrapClient, actor.CancellationToken);
                    }

                    await actor.BootstrapClient.RegisterMethodsForDeviceAsync(
                        this.deviceModel.CloudToDeviceMethods, actor.DeviceState);

                    actor.MoveNext();
                }
                catch (Exception e)
                {
                    this.log.Error("Error while writing the reported properties",
                        () => new { this.deviceId, e });
                }
            }
        }

        private async Task UpdateTwinAsync(Device device, IDeviceClient client, CancellationToken token)
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

            await client.UpdateTwinAsync(device);

            this.log.Debug("Simulated device properties updated", () => { });
        }

        // TODO: we should set this on creation, so we save one Read and one Write operation
        //       https://github.com/Azure/device-simulation-dotnet/issues/88
        private static bool IsTwinNotUpdated(Device device)
        {
            return !device.Twin.ReportedProperties.ContainsKey("Protocol")
                   || !device.Twin.ReportedProperties.ContainsKey("SupportedMethods")
                   || !device.Twin.ReportedProperties.ContainsKey("Telemetry");
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
