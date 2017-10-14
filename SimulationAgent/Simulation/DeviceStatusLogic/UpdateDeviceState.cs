// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    /// <summary>
    /// Periodically update the device state (i.e. sensors data), executing
    /// the script provided in the device model configuration.
    /// </summary>
    public class UpdateDeviceState : IDeviceStatusLogic
    {
        private const string CALC_TELEMETRY = "CalculateRandomizedTelemetry";

        private readonly IScriptInterpreter scriptInterpreter;
        private readonly ILogger log;
        private string deviceId;
        private DeviceModel deviceModel;

        // The timer invoking the Run method
        private readonly ITimer timer;

        // Ensure that setup is called once and only once (which helps also detecting thread safety issues)
        private bool setupDone = false;

        private IDeviceActor context;

        public UpdateDeviceState(
            ITimer timer,
            IScriptInterpreter scriptInterpreter,
            ILogger logger)
        {
            this.timer = timer;
            this.scriptInterpreter = scriptInterpreter;
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
            this.timer.Setup(this.Run, this.deviceModel.Simulation.Script.Interval);
        }

        public void Start()
        {
            this.log.Info("Starting UpdateDeviceState timer",
                () => new { this.context.DeviceId });
            this.timer.Start();
        }

        public void Stop()
        {
            this.log.Info("Stopping UpdateDeviceState timer",
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

            var actor = this.context;
            if (actor.CancellationToken.IsCancellationRequested)
            {
                actor.Stop();
                return;
            }

            this.log.Debug("Checking for the need to compute new telemetry", () => new { this.deviceId, deviceState = actor.DeviceState });

            // Compute new telemetry.
            try
            {
                var scriptContext = new Dictionary<string, object>
                {
                    ["currentTime"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                    ["deviceId"] = this.deviceId,
                    ["deviceModel"] = this.deviceModel.Name
                };

                // until the correlating function has been called; e.g. when increasepressure is called, don't write
                // telemetry until decreasepressure is called for that property.
                if ((bool) actor.DeviceState[CALC_TELEMETRY])
                {
                    this.log.Debug("Updating device telemetry data", () => new { this.deviceId, deviceState = actor.DeviceState });
                    lock (actor.DeviceState)
                    {
                        actor.DeviceState = this.scriptInterpreter.Invoke(
                            this.deviceModel.Simulation.Script,
                            scriptContext,
                            actor.DeviceState);
                    }
                    this.log.Debug("New device telemetry data calculated", () => new { this.deviceId, deviceState = actor.DeviceState });
                }
                else
                {
                    this.log.Debug(
                        "Random telemetry generation is turned off for the actor",
                        () => new { this.deviceId, deviceState = actor.DeviceState });
                }

                // Move state machine forward to update properties and start sending telemetry messages
                if (actor.ActorStatus == Status.UpdatingDeviceState)
                {
                    actor.MoveNext();
                }
                else
                {
                    this.log.Debug(
                        "Already moved state machine forward, running local simulation to generate new property values",
                        () => new { this.deviceId });
                }
            }
            catch (Exception e)
            {
                this.log.Error("UpdateDeviceState failed",
                    () => new { this.deviceId, e.Message, Error = e.GetType().FullName });
            }
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
