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
            this.log.Info("Starting UpdateDeviceState", () => new { this.deviceId });
            this.timer.RunOnce(0);
        }

        public void Stop()
        {
            this.log.Info("Stopping UpdateDeviceState", () => new { this.deviceId });
            this.timer.Cancel();
        }

        public void Run(object context)
        {
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            try
            {
                try
                {
                    this.RunInternal();
                }
                finally
                {
                    var passed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                    if (this.deviceModel != null)
                    {
                        this.timer?.RunOnce(this.deviceModel?.Simulation.Script.Interval.TotalMilliseconds - passed);
                    }
                }
            }
            catch (ObjectDisposedException e)
            {
                this.log.Debug("The simulation was stopped and some of the context is not available", () => new { e });
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
                this.log.Debug("Checking for the need to compute new telemetry",
                    () => new { this.deviceId, deviceState = actor.DeviceState });
                if ((bool) actor.DeviceState[CALC_TELEMETRY])
                {
                    // Compute new telemetry.
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
                        "Random telemetry generation is turned off for the device",
                        () => new { this.deviceId, deviceState = actor.DeviceState });
                }

                // Move state machine forward to start watching twin changes and sending telemetry
                if (actor.ActorStatus == Status.UpdatingDeviceState)
                {
                    actor.MoveNext();
                }
            }
            catch (Exception e)
            {
                this.log.Error("UpdateDeviceState failed", () => new { this.deviceId, e });
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
