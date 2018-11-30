// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState
{
    /// <summary>
    /// Periodically update the device state (i.e. sensors data), executing
    /// the script provided in the device model configuration.
    /// </summary>
    public class UpdateDeviceState
    {
        private readonly ILogger log;
        private readonly IInstance instance;
        private string deviceId;
        private DeviceModel deviceModel;
        private DeviceStateActor deviceStateActor;

        public UpdateDeviceState(
            ILogger logger,
            IInstance instance)
        {
            this.log = logger;
            this.instance = instance;
        }

        public void Init(DeviceStateActor context, string deviceId, DeviceModel deviceModel)
        {
            this.instance.InitOnce();

            this.deviceId = deviceId;
            this.deviceModel = deviceModel;
            this.deviceStateActor = context;

            this.instance.InitComplete();
        }

        public void Run()
        {
            this.instance.InitRequired();

            if (this.deviceStateActor.DeviceState.Has(DeviceStateActor.CALC_TELEMETRY)
                && (bool) this.deviceStateActor.DeviceState.Get(DeviceStateActor.CALC_TELEMETRY))
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
                lock (this.deviceStateActor.DeviceState)
                {
                    foreach (var script in this.deviceModel.Simulation.Scripts)
                    {
                        // call Invoke() to update the internal device state
                        this.deviceStateActor.ScriptInterpreter.Invoke(
                            script,
                            scriptContext,
                            this.deviceStateActor.DeviceState,
                            this.deviceStateActor.DeviceProperties);
                    }

                    // This is inside the lock to avoid exceptions like "Collection was modified"
                    // which could be caused by a method call changing the state.
                    this.log.Debug("New device telemetry data calculated",
                        () => new { this.deviceId, deviceState = this.deviceStateActor.DeviceState });
                }
            }
            else
            {
                this.log.Debug("Random telemetry generation is turned off for the device", () => new { this.deviceId });
            }
        }
    }
}
