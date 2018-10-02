// Copyright (c) Microsoft. All rights reserved. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    public interface ISimulationAgent
    {
        Task RunAsync();
        Task AddDeviceAsync(string name, string modelId);
        Task DeleteDevicesAsync(List<string> ids);
        void Stop();
    }

    public class Agent : ISimulationAgent
    {
        private const int CHECK_INTERVAL_MSECS = 10000;
        private const int DIAGNOSTICS_POLLING_FREQUENCY_DAYS = 1;

        private readonly ILogger log;
        private readonly IDiagnosticsLogger logDiagnostics;
        private readonly ISimulations simulations;
        private readonly ISimulationRunner runner;
        private readonly IRateLimiting rateReporter;
        private readonly IDeviceModels deviceModels;
        private readonly IDevices devices;
        private DateTimeOffset lastPolledTime;
        private Simulation simulation;
        private bool running;

        public Agent(
            ILogger logger,
            IDiagnosticsLogger diagnosticsLogger,
            ISimulations simulations,
            ISimulationRunner runner,
            IRateLimiting rateReporter,
            IDeviceModels deviceModels,
            IDevices devices)
        {
            this.log = logger;
            this.logDiagnostics = diagnosticsLogger;
            this.simulations = simulations;
            this.runner = runner;
            this.rateReporter = rateReporter;
            this.deviceModels = deviceModels;
            this.devices = devices;
            this.running = true;
            this.lastPolledTime = DateTimeOffset.UtcNow;
        }

        public async Task RunAsync()
        {
            this.log.Info("Simulation Agent running");

            // Keep running, checking if the simulation changes
            while (this.running)
            {
                var oldSimulation = this.simulation;

                this.SendSolutionHeartbeatAsync();

                try
                {
                    this.log.Debug("------ Checking for simulation changes ------");

                    var simulationList = await this.simulations.GetListAsync();

                    // currently we support only 1 running simulation so the result should return only 1 item
                    var runningSimulation = simulationList.FirstOrDefault(s => s.ShouldBeRunning);
                    if (runningSimulation == null)
                    {
                        this.log.Debug("No simulations found that should be running. Nothing to do.");
                    }

                    this.log.Debug("Simulation loaded", () => new { runningSimulation });

                    // if the simulation has been removed from storage & we're running, stop the simulation.
                    var id = this.simulation?.Id;
                    var prevSimulation = simulationList.FirstOrDefault(s => s.Id == id);
                    this.CheckForDeletedSimulation(prevSimulation);

                    // if there's a new simulation and it's different from the current one
                    // stop the current one from running & start the new one if it's enabled
                    await this.CheckForChangedSimulationAsync(runningSimulation);

                    // if there's no simulation running but there's one from storage start it
                    await this.CheckForNewSimulationAsync(runningSimulation);

                    // if the current simulation was asked to stop, stop it.
                    await this.CheckForStopOrStartToSimulationAsync();
                }
                catch (Exception e)
                {
                    this.log.Error("Failure reading and starting simulation from storage.", e);
                    this.simulation = oldSimulation;
                }

                if (this.simulation != null && this.simulation.ShouldBeRunning)
                {
                    this.log.Debug("------ Current simulation being run ------");
                    foreach (var model in this.simulation.DeviceModels)
                    {
                        this.log.Debug("Device model", () => new { model });
                    }
                }

                Thread.Sleep(CHECK_INTERVAL_MSECS);
            }
        }

        public void Stop()
        {
            this.simulation = null;
            this.running = false;
            this.runner.Stop();
        }

        public async Task AddDeviceAsync(string deviceId, string modelId)
        {
            if (this.simulation != null && this.IsDeviceModelIdValidAsync(modelId))
            {
                try
                {
                    await this.AddDeviceToSimulationRecordAsync(this.simulation, deviceId, modelId);

                    if (this.running)
                    {
                        this.log.Info("Add device to running simulation");
                        await this.runner.AddDeviceAsync(deviceId, modelId);
                    }
                    else
                    {
                        this.log.Info("Add device to IoT Hub");
                        await this.simulations.AddDeviceAsync(deviceId);
                    }
                }
                catch (Exception e)
                {
                    this.log.Debug("Error while adding new device", () => new { e });
                    throw new Exception("Error while adding a new device");
                }
            }
        }

        public async Task DeleteDevicesAsync(List<string> ids)
        {
            this.log.Info("Update simulation");

            try
            {
                if (this.simulation != null)
                {
                    await this.DeleteDevicesFromSimulationRecordAsync(this.simulation, ids);

                    if (this.running)
                    {
                        this.log.Info("Deleting devices from running simulation");
                        this.runner.DeleteDevices(ids);
                    }
                    else
                    {
                        this.log.Info("Deleting devices from hub");
                        await this.devices.DeleteListAsync(ids);
                    }
                }
            }
            catch (Exception e)
            {
                this.log.Debug("Error while deleting new device", () => new { e });
                throw new Exception("Error while deleting a new device");
            }
        }

        private void CheckForDeletedSimulation(Simulation newSimulation)
        {
            if (newSimulation == null && this.simulation != null)
            {
                this.runner.Stop();

                this.simulation = null;
                this.log.Debug("The current simulation is no longer in storage...");
            }
        }

        private async Task CheckForChangedSimulationAsync(Simulation newSimulation)
        {
            if (newSimulation != null && this.simulation != null &&
                newSimulation.Modified != this.simulation.Modified)
            {
                this.log.Debug("The simulation has been modified, stopping the current " +
                               "simulation and starting the new one if enabled");
                this.runner.Stop();

                this.simulation = newSimulation;

                if (this.simulation.ShouldBeRunning)
                {
                    this.log.Debug("------ Starting simulation ------", () => new { this.simulation });
                    await this.runner.StartAsync(this.simulation);
                    this.log.Debug("------ Simulation started ------", () => new { this.simulation });
                }
            }
        }

        private void SendSolutionHeartbeatAsync()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TimeSpan duration = now - this.lastPolledTime;

            // Send heartbeat every 24 hours
            if (duration.Days >= DIAGNOSTICS_POLLING_FREQUENCY_DAYS)
            {
                this.lastPolledTime = now;
                this.logDiagnostics.LogServiceHeartbeat();
            }
        }

        private async Task CheckForNewSimulationAsync(Simulation newSimulation)
        {
            if (newSimulation != null && this.simulation == null)
            {
                this.simulation = newSimulation;
                if (this.simulation.ShouldBeRunning)
                {
                    this.log.Debug("------ Starting new simulation ------", () => new { this.simulation });
                    this.logDiagnostics.LogServiceStart("Starting new simulation");
                    await this.runner.StartAsync(this.simulation);
                    this.log.Debug("------ New simulation started ------", () => new { this.simulation });
                    this.logDiagnostics.LogServiceStart("New simulation started");
                }
            }
        }

        private async Task CheckForStopOrStartToSimulationAsync()
        {
            // stopped
            if (this.simulation != null && this.simulation.Enabled && !this.simulation.ShouldBeRunning)
            {
                this.simulation.Statistics.AverageMessagesPerSecond = this.rateReporter.GetThroughputForMessages();
                this.simulation.Statistics.TotalMessagesSent = this.runner.TotalMessagesCount;

                this.runner.Stop();

                // Update simulation statistics in storage
                await this.simulations.UpsertAsync(this.simulation);
            }

            // started
            if (this.simulation != null && this.simulation.ShouldBeRunning)
            {
                await this.runner.StartAsync(this.simulation);
            }
        }

        private bool IsDeviceModelIdValidAsync(string modelId)
        {
            var models = this.deviceModels.GetListAsync().Result;

            foreach (var model in models)
            {
                if (modelId.Equals(model.Id, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // TODO: Move the method to Simulations.cs  e.g. this.simulations.AddDevicesAsync
        private async Task AddDeviceToSimulationRecordAsync(Simulation simulation, string deviceId, string modelId)
        {
            DeviceModelRef deviceModel = new DeviceModelRef();
            deviceModel.Id = modelId;
            CustomDeviceRef customDevice = new CustomDeviceRef();
            customDevice.DeviceModel = deviceModel;
            customDevice.DeviceId = deviceId;

            try
            {
                if (!simulation.CustomDevices.ToList().Contains(customDevice))
                {
                    this.log.Info("Update simulation record");
                    simulation.CustomDevices.Add(customDevice);
                    await this.simulations.UpsertAsync(simulation);
                }
                else
                {
                    this.log.Debug("Device already exists in simulation record");
                }
            }
            catch (Exception e)
            {
                this.log.Error("Error while adding new device to simulation record", () => new { e });
                throw new Exception("Error while adding a new device to simulation record");
            }
        }

        // TODO: Move the method to Simulations.cs e.g. this.simulations.RemoveDevicesAsync
        private async Task DeleteDevicesFromSimulationRecordAsync(Simulation simulation, List<string> ids)
        {
            bool shouldUpdateSimulation = false;

            foreach (var id in ids.ToList())
            {
                var deviceRemoved = false;

                // Try to remove device from custom devices list
                foreach (var model in simulation.CustomDevices.ToList())
                {
                    if (id.Equals(model.DeviceId))
                    {
                        this.log.Info("Remove device from custom device list", () => new { id });
                        simulation.CustomDevices.Remove(model);
                        shouldUpdateSimulation = true;
                        deviceRemoved = true;
                        break;
                    }
                }

                if (!deviceRemoved)
                {
                    // Try to remove device from device models list. Update device model count.
                    foreach (var model in simulation.DeviceModels.ToList())
                    {
                        string parsedModelId = id.Substring(0, id.LastIndexOf(('.')));
                        if (parsedModelId.Equals(model.Id))
                        {
                            this.log.Info("Decrement device model count", () => new { id, parsedModelId });
                            model.Count--;
                            if (model.Count <= 0)
                            {
                                this.log.Info("Remove device from device model list", () => new { id });
                                simulation.DeviceModels.Remove(model);
                            }

                            shouldUpdateSimulation = true;
                        }
                    }
                }
            }

            if (shouldUpdateSimulation)
            {
                this.log.Info("Update simulation record in storage");
                await this.simulations.UpsertAsync(simulation);
            }
        }
    }
}
