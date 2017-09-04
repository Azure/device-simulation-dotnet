// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using System.Threading;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    /// <summary>
    /// Periodically update the device state (i.e. sensors data), executing
    /// the script provided in the device model configuration.
    /// </summary>
    public class UpdateDeviceState : IDeviceStatusLogic
    {
        // When connecting to IoT Hub, timeout after 10 seconds
        private static readonly TimeSpan connectionTimeout = TimeSpan.FromSeconds(10);

        private readonly IScriptInterpreter scriptInterpreter;
        private readonly ILogger log;
        private string deviceId;
        private DeviceModel deviceModel;
        private IDevices devices;

        // Ensure that setup is called once and only once (which helps also detecting thread safety issues)
        private bool setupDone = false;

        public UpdateDeviceState(
            IDevices devices,
            IScriptInterpreter scriptInterpreter,
            ILogger logger)
        {
            this.scriptInterpreter = scriptInterpreter;
            this.log = logger;
            this.devices = devices;
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

            var actor = (IDeviceActor) context;
            if (actor.CancellationToken.IsCancellationRequested)
            {
                actor.Stop();
                return;
            }

            // Update the device state.
            try
            {

                var scriptContext = new Dictionary<string, object>
                {
                    ["currentTime"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                    ["deviceId"] = this.deviceId,
                    ["deviceModel"] = this.deviceModel.Name
                };

                this.log.Debug("Updating device status", () => new { this.deviceId, deviceState = actor.DeviceState });
                lock (actor.DeviceState)
                {
                    actor.DeviceState = this.scriptInterpreter.Invoke(
                        this.deviceModel.Simulation.Script,
                        scriptContext,
                        actor.DeviceState);
                }
                this.log.Debug("New device status", () => new { this.deviceId, deviceState = actor.DeviceState });

                this.log.Debug("Checking for desired property updates", () => new { this.deviceId, deviceState = actor.DeviceState });
                // Get device
                var device = this.GetDevice(actor.CancellationToken);
                lock (actor.DeviceState)
                {
                    // check for differences between reported/desired properties, update device and local state
                    if (ChangePropertiesToMatchDesired(device, this.deviceModel))
                        actor.BootstrapClient.UpdateTwinAsync(device).Wait((int)connectionTimeout.TotalMilliseconds);
                }

                // Start sending telemetry messages
                if (actor.ActorStatus == Status.UpdatingDeviceState)
                {
                    actor.MoveNext();
                } else
                    this.log.Debug("Already sending telemetry, running local simulation and watching desired property changes", () => new { this.deviceId});

            }
            catch (Exception e)
            {
                this.log.Error("UpdateDeviceState failed",
                    () => new { this.deviceId, e.Message, Error = e.GetType().FullName });
            }
        }

        private bool ChangePropertiesToMatchDesired(Device device, DeviceModel deviceModel)
        {
            bool differences = false;

            foreach(var item in device.Twin.DesiredProperties)
            {
                if (device.Twin.ReportedProperties.ContainsKey(item.Key))
                {
                    if (device.Twin.ReportedProperties[item.Key].ToString() != device.Twin.DesiredProperties[item.Key].ToString())
                    {
                        //match reported properties
                        device.Twin.ReportedProperties[item.Key] = device.Twin.DesiredProperties[item.Key];

                        //match local properties
                        if (deviceModel.Properties.ContainsKey(item.Key))
                            deviceModel.Properties[item.Key] = device.Twin.DesiredProperties[item.Key];

                        differences = true;
                    }
                }
            }

            return differences;
        }

        private Device GetDevice(CancellationToken token)
        {
            var task = this.devices.GetAsync(this.deviceId);
            task.Wait((int)connectionTimeout.TotalMilliseconds, token);
            return task.Result;
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
