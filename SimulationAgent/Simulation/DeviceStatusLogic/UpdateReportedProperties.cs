// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    public class UpdateReportedProperties : IDeviceStatusLogic
    {
        // Twin update frequency
        private const int UPDATE_FREQUENCY_MSECS = 30000;

        private readonly IDevices devices;
        private string deviceId;
        private DeviceModel deviceModel;

        // The timer invoking the Run method
        private readonly ITimer timer;

        private readonly ILogger log;

        // Ensure that setup is called once and only once (which helps also detecting thread safety issues)
        private bool setupDone = false;

        private IDeviceActor context;

        public UpdateReportedProperties(
            ITimer timer,
            IDevices devices,
            ILogger logger)
        {
            this.timer = timer;
            this.log = logger;
            this.devices = devices;
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
        }

        public void Start()
        {
            this.log.Info("Starting UpdateReportedProperties", () => new { this.deviceId });
            this.timer.RunOnce(0);
        }

        public void Stop()
        {
            this.log.Info("Stopping UpdateReportedProperties", () => new { this.deviceId });
            this.timer.Cancel();
        }

        public void Run(object context)
        {
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            try
            {
                try
                {
                    this.RunInternalAsync().Wait();
                }
                finally
                {
                    var passed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                    this.timer?.RunOnce(UPDATE_FREQUENCY_MSECS - passed);
                }
            }
            catch (ObjectDisposedException e)
            {
                this.log.Debug("The simulation was stopped and some of the context is not available", () => new { e });
            }
        }

        private async Task RunInternalAsync()
        {
            this.ValidateSetup();

            var actor = this.context;
            if (actor.CancellationToken.IsCancellationRequested)
            {
                actor.Stop();
                return;
            }

            try
            {
                this.log.Debug("Checking for desired property updates & update reported properties",
                    () => new { this.deviceId, deviceState = actor.DeviceState });

                // Get device from IoT Hub registry
                var device = await this.devices.GetAsync(this.deviceId, true, actor.CancellationToken);

                var differences = false;
                lock (actor.DeviceState)
                {
                    // TODO: the device model should define whether the local state or the
                    //       desired state wins, i.e.where is the master value
                    // https://github.com/Azure/device-simulation-dotnet/issues/76

                    // update reported properties with any state changes (either from desired prop
                    // changes, methods, etc.)
                    if (this.ChangeTwinPropertiesToMatchDesired(device, actor.DeviceState))
                        differences = true;

                    // check for differences between reported/desired properties, update reported
                    // properties with desired property values
                    if (this.ChangeTwinPropertiesToMatchActorState(device, actor.DeviceState))
                        differences = true;
                }

                if (differences)
                {
                    await actor.BootstrapClient.UpdateTwinAsync(device);
                }

                // Move state machine forward to start sending telemetry
                if (actor.ActorStatus == Status.UpdatingReportedProperties)
                {
                    actor.MoveNext();
                }
            }
            catch (Exception e)
            {
                this.log.Error("UpdateReportedProperties failed",
                    () => new { this.deviceId, e });
            }
        }

        private bool ChangeTwinPropertiesToMatchActorState(Device device, Dictionary<string, object> actorState)
        {
            bool differences = false;

            // TODO: revisit how twin props are stored, e.g.rather than looping actorState, have a dedicated propertybag
            // https://github.com/Azure/device-simulation-dotnet/issues/77

            foreach (var item in actorState)
            {
                if (device.Twin.ReportedProperties.ContainsKey(item.Key))
                {
                    if (device.Twin.ReportedProperties[item.Key].ToString() != actorState[item.Key].ToString())
                    {
                        // Update the Hub twin to match the actor state
                        device.Twin.ReportedProperties[item.Key] = actorState[item.Key].ToString();
                        differences = true;
                    }
                }
            }
            return differences;
        }

        private bool ChangeTwinPropertiesToMatchDesired(Device device, Dictionary<string, object> actorState)
        {
            bool differences = false;

            foreach (var item in device.Twin.DesiredProperties)
            {
                if (device.Twin.ReportedProperties.ContainsKey(item.Key))
                {
                    if (device.Twin.ReportedProperties[item.Key].ToString() != device.Twin.DesiredProperties[item.Key].ToString())
                    {
                        // update the hub reported property to match match hub desired property
                        device.Twin.ReportedProperties[item.Key] = device.Twin.DesiredProperties[item.Key];

                        // update actor state property to match hub desired changes
                        if (actorState.ContainsKey(item.Key))
                            actorState[item.Key] = device.Twin.DesiredProperties[item.Key];

                        differences = true;
                    }
                }
            }

            return differences;
        }

        private void ValidateSetup()
        {
            if (!this.setupDone)
            {
                this.log.Error("Application error: Setup() must be invoked before Run().",
                    () => new { this.deviceId, this.deviceModel });
                throw new DeviceActorAlreadyInitializedException();
            }
        }
    }
}
