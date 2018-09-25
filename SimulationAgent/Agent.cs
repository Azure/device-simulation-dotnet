// Copyright (c) Microsoft. All rights reserved. 

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    public interface ISimulationAgent
    {
        Task StartAsync();
        void Stop();
    }

    public class Agent : ISimulationAgent
    {
        private const int CHECK_INTERVAL_MSECS = 10000;
        private const int DIAGNOSTICS_POLLING_FREQUENCY_DAYS = 1;
        
        // How often (minimum) to log simulation statistics
        private const int STATS_INTERVAL_MSECS = 15000;

        private readonly IFactory factory;
        private readonly ILogger log;
        private readonly IDiagnosticsLogger logDiagnostics;
        private readonly ISimulations simulations;
        private readonly ISimulationRunner runner;
        private readonly IRateLimiting rateReporter;
        private readonly IDeviceModels deviceModels;
        private readonly IDevices devices;
        private readonly ConcurrentDictionary<string, ISimulationManager> simulationManagers;

        // List of all the actors managing the devices state, indexed by Simulation ID + Device ID (string concat)
        private readonly ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors;

        // Contains all the actors responsible to connect the devices, indexed by Simulation ID + Device ID (string concat)
        private readonly ConcurrentDictionary<string, IDeviceConnectionActor> deviceConnectionActors;

        // Contains all the actors sending telemetry, indexed by Simulation ID + Device ID (string concat)
        private readonly ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors;

        // Contains all the actors sending device property updates to Azure IoT Hub, indexed by Simulation ID + Device ID (string concat)
        private readonly ConcurrentDictionary<string, IDevicePropertiesActor> devicePropertiesActors;

        private DateTimeOffset lastPolledTime;
        private bool running;
        private long lastStatsTime;

        public Agent(
            ILogger logger,
            IDiagnosticsLogger diagnosticsLogger,
            ISimulations simulations,
            ISimulationRunner runner,
            IRateLimiting rateReporter,
            IDeviceModels deviceModels,
            IDevices devices,
            IFactory factory)
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

            this.simulationManagers = new ConcurrentDictionary<string, ISimulationManager>();
            this.deviceStateActors = new ConcurrentDictionary<string, IDeviceStateActor>();
            this.deviceConnectionActors = new ConcurrentDictionary<string, IDeviceConnectionActor>();
            this.deviceTelemetryActors = new ConcurrentDictionary<string, IDeviceTelemetryActor>();
            this.devicePropertiesActors = new ConcurrentDictionary<string, IDevicePropertiesActor>();
        }

        public async Task RunAsync()
        {
            try
            {
                // Keep running, checking if the simulation changes
                while (this.running)
                {
                    this.log.Debug("Starting simulation agent loop",
                        () => new { SimulationsCount = this.simulationManagers.Count });

                    // Get list of active simulations. Active simulations are already partitioned.
                    IList<Simulation> activeSimulations = (await this.simulations.GetListAsync())
                        .Where(x => x.IsActiveNow).ToList();
                    this.log.Debug("Active simulations loaded", () => new { activeSimulations.Count });

                    // Create new simulation managers (if needed), and run them
                    await this.CreateSimulationManagersAsync(activeSimulations);
                    await this.RunSimulationManagersMaintenanceAsync();

                    this.StopInactiveSimulations(activeSimulations);

                    Thread.Sleep(CHECK_INTERVAL_MSECS);
                }
            }
            catch (Exception e)
            {
                this.log.Error("A critical error occurred in the simulation agent", e);
                this.Stop();
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
                        this.runner.AddDevice(deviceId, modelId);
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

        private async Task CreateSimulationManagersAsync(IEnumerable<Simulation> activeSimulations)
        {
            // Skip simulations not ready or already with a manager
            var list = activeSimulations
                .Where(x => x.ShouldBeRunning && !this.simulationManagers.ContainsKey(x.Id));

            foreach (var simulation in list)
            {
                this.log.Info("Creating new simulation manager...", () => new { SimulationId = simulation.Id });

                try
                {
                    var manager = this.factory.Resolve<ISimulationManager>();
                    await manager.InitAsync(
                        simulation,
                        this.deviceStateActors,
                        this.deviceConnectionActors,
                        this.deviceTelemetryActors,
                        this.devicePropertiesActors);

                    this.simulationManagers[simulation.Id] = manager;

                    this.log.Info("New simulation manager created", () => new { SimulationId = simulation.Id });
                }
                catch (Exception e)
                {
                    this.log.Error("Failed to create simulation manager, will retry", () => new { simulation.Id, e });
                }
            }
        }

        private async Task RunSimulationManagersMaintenanceAsync()
        {
            var printStats = false;
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now - this.lastStatsTime > STATS_INTERVAL_MSECS)
            {
                printStats = true;
                this.lastStatsTime = now;
            }

            // TODO: check if these can run in parallel
            foreach (var manager in this.simulationManagers)
            {
                await TryToAsync(manager.Value.HoldAssignedPartitionsAsync(),
                    e => this.log.Error("An unexpected error occurred while renewing partition locks", e));

                await TryToAsync(manager.Value.AssignNewPartitionsAsync(),
                    e => this.log.Error("An unexpected error occurred while assigning new partitions", e));

                await TryToAsync(manager.Value.HandleAssignedPartitionChangesAsync(),
                    e => this.log.Error("An unexpected error occurred while handling partition changes", e));

                await TryToAsync(manager.Value.UpdateThrottlingLimitsAsync(),
                    e => this.log.Error("An unexpected error occurred while updating the throttling limits", e));

                if (printStats) manager.Value.PrintStats();
            }

            async Task TryToAsync(Task task, Action<Exception> onException)
            {
                try
                {
                    await task;
                }
                catch (Exception e)
                {
                    onException.Invoke(e);
                }
            }
        }

        private void CheckForDeletedSimulation(Services.Models.Simulation newSimulation)
        {
            if (newSimulation == null && this.simulation != null)
            {
                this.runner.Stop();

                this.simulation = null;
                this.log.Debug("No simulation found in storage...");
            }
        }

        private async Task CheckForChangedSimulationAsync(Services.Models.Simulation newSimulation)
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
