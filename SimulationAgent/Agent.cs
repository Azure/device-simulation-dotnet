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
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.SimulationThreads;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    public interface ISimulationAgent
    {
        Task StartAsync();
        Task AddDeviceAsync(string name, string modelId);
        Task DeleteDevicesAsync(List<string> ids);
        void Stop();
    }

    public class Agent : ISimulationAgent
    {
        // Wait a few seconds after checking if there are new simulations
        // or new devices, to avoid overloading the database with queries.
        private const int PAUSE_AFTER_CHECK_MSECS = 20000;
        private const int SHUTDOWN_WAIT_INTERVAL_MSECS = 1000;
        private const int SHUTDOWN_WAIT_BEFORE_STOPPING_THREADS_MSECS = 3000;
        private const int DIAGNOSTICS_POLLING_FREQUENCY_DAYS = 1;

        // How often (minimum) to log simulation statistics
        private const int STATS_INTERVAL_MSECS = 15000;

        private readonly ISimulationConcurrencyConfig simulationConcurrencyConfig;
        private readonly ILogger log;
        private readonly IDiagnosticsLogger logDiagnostics;
        private readonly ISimulations simulations;
        private readonly IFactory factory;
        private DateTimeOffset lastPolledTime;
        private bool running;

        // The thread responsible for updating devices/sensors state
        private Thread devicesStateThread;
        private IDeviceStateTask devicesStateTask;

        // The thread responsible for connecting all the devices to Azure IoT Hub
        private Thread devicesConnectionThread;
        private IDeviceConnectionTask devicesConnectionTask;

        // The thread responsible for sending device property updates to Azure IoT Hub
        private Thread devicesPropertiesThread;
        private IUpdatePropertiesTask devicesPropertiesTask;

        // Array of threads used to send telemetry
        private Thread[] devicesTelemetryThreads;
        private List<IDeviceTelemetryTask> devicesTelemetryTasks;

        // List of simulation managers, one for each simulation
        private readonly ConcurrentDictionary<string, ISimulationManager> simulationManagers;

        // List of all the actors managing the devices state, indexed by Simulation ID + Device ID (string concat)
        private readonly ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors;

        // Contains all the actors responsible to connect the devices, indexed by Simulation ID + Device ID (string concat)
        private readonly ConcurrentDictionary<string, IDeviceConnectionActor> deviceConnectionActors;

        // Contains all the actors sending telemetry, indexed by Simulation ID + Device ID (string concat)
        private readonly ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors;

        // Contains all the actors sending device property updates to Azure IoT Hub, indexed by Simulation ID + Device ID (string concat)
        private readonly ConcurrentDictionary<string, IDevicePropertiesActor> devicePropertiesActors;

        // Flag signaling whether the simulation is starting (to reduce blocked threads)
        private bool startingOrStopping;

        // Used to stop the threads
        private CancellationTokenSource runningToken;
        private long lastStatsTime;

        public Agent(
            ISimulationConcurrencyConfig simulationConcurrencyConfig,
            ISimulations simulations,
            IFactory factory,
            ILogger logger,
            IDiagnosticsLogger diagnosticsLogger)
        {
            this.simulationConcurrencyConfig = simulationConcurrencyConfig;
            this.simulations = simulations;
            this.factory = factory;
            this.log = logger;
            this.logDiagnostics = diagnosticsLogger;

            this.startingOrStopping = false;
            this.running = false;
            this.runningToken = new CancellationTokenSource();
            this.lastPolledTime = DateTimeOffset.UtcNow;
            this.lastStatsTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            this.simulationManagers = new ConcurrentDictionary<string, ISimulationManager>();
            this.deviceStateActors = new ConcurrentDictionary<string, IDeviceStateActor>();
            this.deviceConnectionActors = new ConcurrentDictionary<string, IDeviceConnectionActor>();
            this.deviceTelemetryActors = new ConcurrentDictionary<string, IDeviceTelemetryActor>();
            this.devicePropertiesActors = new ConcurrentDictionary<string, IDevicePropertiesActor>();
        }

        public Task StartAsync()
        {
            if (this.startingOrStopping || this.running)
            {
                this.log.Error("The simulation agent can be started only once");
                return Task.CompletedTask;
            }

            this.running = false;
            this.startingOrStopping = true;
            this.runningToken.Cancel();
            this.runningToken = new CancellationTokenSource();

            this.TryToStartThreads();

            this.running = true;
            this.startingOrStopping = false;

            return this.RunAsync();
        }

        public void Stop()
        {
            // TODO: why is this needed?
            while (this.startingOrStopping)
                Thread.Sleep(SHUTDOWN_WAIT_INTERVAL_MSECS);

            this.startingOrStopping = true;
            this.log.Write("Stopping simulation agent...");

            this.running = false;

            // Signal threads to stop
            this.runningToken.Cancel();
            this.runningToken = new CancellationTokenSource();

            // Allow some time to pass before trying to stop threads
            Thread.Sleep(SHUTDOWN_WAIT_BEFORE_STOPPING_THREADS_MSECS);
            this.TryToStopThreads();
        }

        private async Task RunAsync()
        {
            try
            {
                while (this.running)
                {
                    this.SendSolutionHeartbeatAsync();

                    this.log.Debug("Starting simulation agent loop",
                        () => new { SimulationsCount = this.simulationManagers.Count });

                    // Get the list of active simulations. Active simulations are already partitioned.
                    IList<Simulation> activeSimulations = (await this.simulations.GetListAsync())
                        .Where(x => x.IsActiveNow).ToList();
                    this.log.Debug("Active simulations loaded", () => new { activeSimulations.Count });

                    await this.CreateSimulationManagersAsync(activeSimulations);
                    await this.RunSimulationManagersMaintenanceAsync();
                    this.StopInactiveSimulations(activeSimulations);

                    Thread.Sleep(PAUSE_AFTER_CHECK_MSECS);
                }
            }
            catch (Exception e)
            {
                this.log.Error("A critical error occurred in the simulation agent", e);
                this.Stop();
            }
        }

        // TODO: Implement support for adding devices to a running simulation.
        //       This functionality is needed for Remote Monitoring, but the
        //       initial implementation of large-scale device simulation
        //       does not support this because we do not have a design for
        //       how to modify existing partitions at runtime. Implementation
        //       of this feature is pending a design for this.
        public async Task AddDeviceAsync(string deviceId, string modelId)
        {
            return;
        }

        // TODO: Implement support for removing devices from a running simulation.
        //       This functionality is needed for Remote Monitoring, but the
        //       initial implementation of large-scale device simulation
        //       does not support this because we do not have a design for
        //       how to modify existing partitions at runtime. Implementation
        //       of this feature is pending a design for this.
        public async Task DeleteDevicesAsync(List<string> ids)
        {
            return;
        }

        private void StopInactiveSimulations(IList<Simulation> activeSimulations)
        {
            // Get a list of all managers that are not active in storage
            var activeIds = activeSimulations.Select(simulation => simulation.Id).ToList();
            var managersToStop = this.simulationManagers.Where(x => !activeIds.Contains(x.Key)).ToList();

            foreach (var manager in managersToStop)
            {
                manager.Value.TearDown();
                if (!this.simulationManagers.TryRemove(manager.Key, out _))
                {
                    this.log.Error("Unable to remove simulation manager from the list of managers",
                        () => new { SimulationId = manager.Key });
                }
            }
        }

        private async Task CreateSimulationManagersAsync(IEnumerable<Simulation> activeSimulations)
        {
            // Skip simulations not ready or already with a manager
            var activeSimulationlist = activeSimulations
                .Where(x => x.ShouldBeRunning && !this.simulationManagers.ContainsKey(x.Id));

            foreach (var simulation in activeSimulationlist)
            {
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

            // TODO: determine if these can be run in parallel
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

        private void TryToStartThreads()
        {
            this.devicesStateTask = this.factory.Resolve<IDeviceStateTask>();
            this.devicesStateThread = new Thread(
                () => this.devicesStateTask.Run(this.deviceStateActors, this.runningToken.Token));

            this.devicesConnectionTask = this.factory.Resolve<IDeviceConnectionTask>();
            this.devicesConnectionThread = new Thread(
                () => this.devicesConnectionTask.RunAsync(
                    this.simulationManagers,
                    this.deviceConnectionActors,
                    this.runningToken.Token));

            this.devicesPropertiesTask = this.factory.Resolve<IUpdatePropertiesTask>();
            this.devicesPropertiesThread = new Thread(
                () => this.devicesPropertiesTask.RunAsync(
                    this.simulationManagers,
                    this.devicePropertiesActors,
                    this.runningToken.Token));

            // State
            try
            {
                this.devicesStateThread.Start();
            }
            catch (Exception e)
            {
                this.log.Error("Unable to start the device-state thread", e);
                throw new Exception("Unable to start the device-state thread", e);
            }

            // Connection
            try
            {
                this.devicesConnectionThread.Start();
            }
            catch (Exception e)
            {
                this.log.Error("Unable to start the device-connection thread", e);
                throw new Exception("Unable to start the device-connection thread", e);
            }

            // Properties
            try
            {
                this.devicesPropertiesThread.Start();
            }
            catch (Exception e)
            {
                this.log.Error("Unable to start the device-properties thread", e);
                throw new Exception("Unable to start the device-properties thread", e);
            }

            // Telemetry
            try
            {
                var count = this.simulationConcurrencyConfig.TelemetryThreads;

                this.devicesTelemetryThreads = new Thread[count];
                this.devicesTelemetryTasks = new List<IDeviceTelemetryTask>();
                for (int i = 0; i < count; i++)
                {
                    var task = this.factory.Resolve<IDeviceTelemetryTask>();
                    this.devicesTelemetryTasks.Add(task);

                    this.devicesTelemetryThreads[i] = new Thread(
                        () => task.RunAsync(this.deviceTelemetryActors, i + 1, count, this.runningToken.Token));
                    this.devicesTelemetryThreads[i].Start();
                }
            }
            catch (Exception e)
            {
                this.log.Error("Unable to start the device-telemetry threads", e);
                throw new Exception("Unable to start the device-telemetry threads", e);
            }
        }

        private void TryToStopThreads()
        {
            // State
            try
            {
                this.devicesStateThread.Interrupt();
            }
            catch (Exception e)
            {
                this.log.Warn("Unable to stop the devices state thread in a clean way", e);
            }

            // Connection
            try
            {
                this.devicesConnectionThread.Interrupt();
            }
            catch (Exception e)
            {
                this.log.Warn("Unable to stop the connections thread in a clean way", e);
            }

            // Properties
            try
            {
                this.devicesPropertiesThread.Interrupt();
            }
            catch (Exception e)
            {
                this.log.Warn("Unable to stop the devices state thread in a clean way", e);
            }

            // Telemetry
            this.devicesTelemetryTasks.Clear();
            for (int i = 0; i < this.devicesTelemetryThreads.Length; i++)
            {
                try
                {
                    if (this.devicesTelemetryThreads[i] != null)
                    {
                        this.devicesTelemetryThreads[i].Interrupt();
                    }
                }
                catch (Exception e)
                {
                    this.log.Warn("Unable to stop the telemetry thread in a clean way", () => new { threadNumber = i, e });
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
