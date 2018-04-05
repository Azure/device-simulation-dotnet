// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState
{
    /// <summary>
    /// Periodically update the device state (i.e. sensors data), executing
    /// the script provided in the device model configuration.
    /// </summary>
    public class UpdateDeviceState
    {
        private readonly IScriptInterpreter scriptInterpreter;
        private readonly ILogger log;
        private bool setupDone;
        private string deviceId;
        private DeviceModel deviceModel;
        private DeviceStateActor context;

        public UpdateDeviceState(
            IScriptInterpreter scriptInterpreter,
            ILogger logger)
        {
            this.scriptInterpreter = scriptInterpreter;
            this.log = logger;
            this.setupDone = false;
        }

        public void Setup(DeviceStateActor context, string deviceId, DeviceModel deviceModel)
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

        public void Run()
        {
            if ((bool) this.context.DeviceState.Get(DeviceStateActor.CALC_TELEMETRY))
            {
                this.log.Debug("Updating device telemetry data", () => new { this.deviceId });

                var scriptContext = new Dictionary<string, object>
                {
                    ["currentTime"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                    ["deviceId"] = this.deviceId,
                    ["deviceModel"] = this.deviceModel.Name
                };

                // Lock the state until all the simulation scripts are complete, so that for example
                // telemetry cannot be sent in the middle of some script running
                lock (this.context.DeviceState)
                {
                    foreach (var script in this.deviceModel.Simulation.Scripts)
                    {
                        // call Invoke() which to update the internal device state
                        this.scriptInterpreter.Invoke(
                            script,
                            scriptContext,
                            this.context.DeviceState,
                            this.context.DeviceProperties);
                    }

                    // This is inside the lock to avoid exceptions like "Collection was modified"
                    // which could be caused by a method call changing the state.
                    this.log.Debug("New device telemetry data calculated",
                        () => new { this.deviceId, deviceState = this.context.DeviceState });
                }
            }
            else
            {
                this.log.Debug("Random telemetry generation is turned off for the device", () => new { this.deviceId });
            }
        }
    }
}
