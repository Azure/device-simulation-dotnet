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
            if ((bool) this.context.DeviceState[DeviceStateActor.CALC_TELEMETRY])
            {
                this.log.Debug("Updating device telemetry data", () => new { this.deviceId });

                var scriptContext = new Dictionary<string, object>
                {
                    ["currentTime"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                    ["deviceId"] = this.deviceId,
                    ["deviceModel"] = this.deviceModel.Name
                };

                lock (this.context.DeviceState)
                {
                    this.context.DeviceState = this.scriptInterpreter.Invoke(
                        this.deviceModel.Simulation.Script,
                        scriptContext,
                        this.context.DeviceState);
                }

                this.log.Debug("New device telemetry data calculated", () => new { this.deviceId, deviceState = this.context.DeviceState });
            }
            else
            {
                this.log.Debug("Random telemetry generation is turned off for the device", () => new { this.deviceId });
            }
        }
    }
}