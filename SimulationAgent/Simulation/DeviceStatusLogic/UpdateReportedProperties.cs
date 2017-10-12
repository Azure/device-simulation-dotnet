// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    public class UpdateReportedProperties : IDeviceStatusLogic
    {
        // When connecting to IoT Hub, timeout after 10 seconds
        private static readonly TimeSpan connectionTimeout = TimeSpan.FromSeconds(10);

        private IDevices devices;
        private string deviceId;
        private DeviceModel deviceModel;

        private readonly ILogger log;

        // Ensure that setup is called once and only once (which helps also detecting thread safety issues)
        private bool setupDone = false;

        public UpdateReportedProperties(
            IDevices devices,
            ILogger logger)
        {
            this.log = logger;
            this.devices = devices;
        }

        public void Run(object context)
        {
            try
            {
                this.ValidateSetup();

                var actor = (IDeviceActor) context;
                if (actor.CancellationToken.IsCancellationRequested)
                {
                    actor.Stop();
                    return;
                }

                //TODO: Here we should pause the timer in case the device takes too long to pull from the hub

                this.log.Debug(
                    "Checking for desired property updates & updated reported properties",
                    () => new
                    {
                        this.deviceId,
                        deviceState = actor.DeviceState
                    });

                // Get device
                var device = this.GetDevice(actor.CancellationToken);
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
                    actor.BootstrapClient.UpdateTwinAsync(device).Wait((int) connectionTimeout.TotalMilliseconds);
                }

                // Move state machine forward to start sending telemetry messages if needed
                if (actor.ActorStatus == Status.UpdatingReportedProperties)
                {
                    actor.MoveNext();
                }
                else
                {
                    this.log.Debug(
                        "Already moved state machine forward, continuing to check for desired property changes",
                        () => new { this.deviceId });
                }
            }
            catch (Exception e)
            {
                this.log.Error("UpdateReportedProperties failed",
                    () => new { this.deviceId, e });
            }
            finally
            {
                //TODO: Here we should unpause the timer - this same thing should be done in all state machine methods
            }
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
                    () => new { this.deviceId, this.deviceModel });
                throw new DeviceActorAlreadyInitializedException();
            }
        }
    }
}
