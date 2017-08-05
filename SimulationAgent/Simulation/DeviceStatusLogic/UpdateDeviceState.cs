// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    /// <summary>
    /// Periodically update the device state (i.e. sensors data), executing
    /// the script provided in the device type configuration.
    /// </summary>
    public class UpdateDeviceState : IDeviceStatusLogic
    {
        private readonly ILogger log;
        private readonly IScriptInterpreter scriptInterpreter;
        private string deviceId;
        private DeviceType deviceType;

        public UpdateDeviceState(
            ILogger logger,
            IScriptInterpreter scriptInterpreter)
        {
            this.log = logger;
            this.scriptInterpreter = scriptInterpreter;
        }

        public void Setup(string deviceId, DeviceType deviceType)
        {
            this.deviceId = deviceId;
            this.deviceType = deviceType;
        }

        public void Run(object context)
        {
            this.SetupRequired();

            var actor = (IDeviceActor) context;
            if (actor.CancellationToken.IsCancellationRequested)
            {
                actor.Stop();
                return;
            }

            var scriptContext = new Dictionary<string, object>
            {
                ["currentTime"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                ["deviceId"] = this.deviceId,
                ["deviceType"] = this.deviceType.Name
            };

            this.log.Debug("Updating device status", () => new { this.deviceId, deviceState = actor.DeviceState });

            lock (actor.DeviceState)
            {
                actor.DeviceState = this.scriptInterpreter.Invoke(
                    this.deviceType.DeviceState.SimulationScript,
                    scriptContext,
                    actor.DeviceState);
            }

            this.log.Debug("New device status", () => new { this.deviceId, deviceState = actor.DeviceState });

            // Start sending telemetry messages
            if (actor.ActorStatus == Status.UpdatingDeviceState)
            {
                actor.MoveNext();
            }
        }

        private void SetupRequired()
        {
            if (this.deviceId == null || this.deviceType == null)
            {
                throw new Exception("Application error: Setup() must be invoked before Run().");
            }
        }
    }
}
