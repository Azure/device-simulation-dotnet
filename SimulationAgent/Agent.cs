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

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    public interface ISimulationAgent
    {
        Task StartAsync();
        void Stop();
    }

    public class Agent : ISimulationAgent
    {
        // Wait some seconds after checking if there are new simulations
        // or new devices, to avoid overloading the database with queries
        private const int PAUSE_AFTER_CHECK_MSECS = 20000;

        // How often (minimum) to log simulation statistics
        private const int STATS_INTERVAL_MSECS = 15000;

        // Global thread settings, not specific to any simulation
        private readonly IAppConcurrencyConfig appConcurrencyConfig;

        private readonly ISimulations simulations;
        private readonly ILogger log;
        private readonly IFactory factory;

        // The thread responsible for updating devices/sensors state
        private Thread devicesStateThread;
        private IDeviceStateTask devicesStateTask;

        // The thread responsible for connecting all the devices to Azure IoT Hub
        private Thread devicesConnectionThread;
        private IDeviceConnectionTask devicesConnectionTask;

        // Array of threads used to send telemetry
        private Thread[] devicesTelemetryThreads;
        private List<IDeviceTelemetryTask> devicesTelemetryTasks;

        // The thread responsible for sending device property updates to Azure IoT Hub
        private Thread devicesPropertiesThread;
        private IUpdatePropertiesTask devicesPropertiesTask;

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

        // Flag signaling whether the agent has started and is running (to avoid contentions)
        private bool running;

        // Used to stop the threads
        private CancellationTokenSource runningToken;
        private long lastStatsTime;

        public Agent(
            IAppConcurrencyConfig appConcurrencyConfig,
            ISimulations simulations,
            IFactory factory,
            ILogger logger)
        {
            this.appConcurrencyConfig = appConcurrencyConfig;
            this.simulations = simulations;
            this.factory = factory;
            this.log = logger;

            this.startingOrStopping = false;
            this.running = false;
            this.runningToken = new CancellationTokenSource();
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

            // Start thread updating each device state
            this.TryToStartStateThread();

            // Start thread managing devices connection
            this.TryToStartConnectionThread();

            // Start thread sending devices telemetry
            this.TryToStartTelemetryThreads();

            // Start thread uploading reported properties
            this.TryToStartReportedPropertiesThread();

            this.running = true;
            this.startingOrStopping = false;

            return this.RunAsync();
        }

        public void Stop()
        {
            while (this.startingOrStopping)
            {
                Thread.Sleep(1000);
            }

            this.startingOrStopping = true;
            this.log.Write("Stopping simulation agent...");

            this.running = false;

            // Signal threads to stop
            this.runningToken.Cancel();
            this.runningToken = new CancellationTokenSource();

            // Allow 3 seconds to complete before stopping the threads
            Thread.Sleep(3000);
            this.TryToStopStateThread();
            this.TryToStopConnectionThread();
            this.TryToStopTelemetryThreads();
            this.TryToStopReportedPropertiesThread();
        }

        private async Task RunAsync()
        {
            while (this.running)
            {
                // Get list of active simulations. Active simulations are already partioned.
                IList<Simulation> activeSimulations = await this.GetActiveSimulationsAsync();

                await this.StartNewSimulations(activeSimulations);
                await this.MonitorClusterSizeAsync();
                await this.MonitorDevicePartitionsAsync();
                this.StopInactiveSimulations(activeSimulations);
                this.PrintStats();

                Thread.Sleep(PAUSE_AFTER_CHECK_MSECS);
            }
        }

        private async Task<IList<Simulation>> GetActiveSimulationsAsync()
        {
            IList<Simulation> activeSimulations = (await this.simulations.GetListAsync())
                .Where(x => x.ShouldBeRunning() && x.PartitioningComplete).ToList();
            return activeSimulations;
        }

        private async Task StartNewSimulations(IList<Simulation> activeSimulations)
        {
            foreach (var simulation in activeSimulations)
            {
                if (this.simulationManagers.ContainsKey(simulation.Id)) continue;

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
                }
                catch (Exception e)
                {
                    this.log.Error("Failed to start simulation", () => new { simulation.Id, e });
                }
            }
        }

        private async Task MonitorClusterSizeAsync()
        {
            // TODO: this can be optimized, fetching all the information just once
            //       and passing it to the list of managers
            foreach (var simulationManager in this.simulationManagers)
            {
                await simulationManager.Value.SyncClusterSizeAsync();
            }
        }

        private async Task MonitorDevicePartitionsAsync()
        {
            // TODO: this can be optimized, fetching all the information just once
            //       and passing it to the list of managers
            foreach (var manager in this.simulationManagers)
            {
                await manager.Value.ManageDevicePartitionsAsync();
            }
        }

        private void StopInactiveSimulations(IList<Simulation> activeSimulations)
        {
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

        private void PrintStats()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (now - this.lastStatsTime < STATS_INTERVAL_MSECS) return;
            this.lastStatsTime = now;

            foreach (var manager in this.simulationManagers)
            {
                manager.Value.PrintStats();
            }
        }

        // =============== STATE ===============

        private void TryToStartStateThread()
        {
            this.devicesStateTask = this.factory.Resolve<IDeviceStateTask>();
            this.devicesStateThread = new Thread(
                () => this.devicesStateTask.Run(this.deviceStateActors, this.runningToken.Token));

            try
            {
                this.devicesStateThread.Start();
            }
            catch (Exception e)
            {
                this.log.Error("Unable to start the device state thread", e);
                throw new Exception("Unable to start the device state thread", e);
            }
        }

        private void TryToStopStateThread()
        {
            try
            {
                this.devicesStateThread.Interrupt();
            }
            catch (Exception e)
            {
                this.log.Warn("Unable to stop the devices state thread in a clean way", e);
            }
        }

        // =============== CONNECTION ===============

        private void TryToStartConnectionThread()
        {
            this.devicesConnectionTask = this.factory.Resolve<IDeviceConnectionTask>();
            this.devicesConnectionThread = new Thread(
                () => this.devicesConnectionTask.RunAsync(
                    this.simulationManagers,
                    this.deviceConnectionActors,
                    this.runningToken.Token));
            try
            {
                this.devicesConnectionThread.Start();
            }
            catch (Exception e)
            {
                this.log.Error("Unable to start the device connection thread", e);
                throw new Exception("Unable to start the device connection thread", e);
            }
        }

        private void TryToStopConnectionThread()
        {
            try
            {
                this.devicesConnectionThread.Interrupt();
            }
            catch (Exception e)
            {
                this.log.Warn("Unable to stop the connections thread in a clean way", e);
            }
        }

        // =============== TELEMETRY ===============

        private void TryToStartTelemetryThreads()
        {
            try
            {
                var count = this.appConcurrencyConfig.TelemetryThreads;

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
                this.log.Error("Unable to start the telemetry threads", e);
                throw new Exception("Unable to start the telemetry threads", e);
            }
        }

        private void TryToStopTelemetryThreads()
        {
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

        // =============== TWIN ===============

        private void TryToStartReportedPropertiesThread()
        {
            this.devicesPropertiesTask = this.factory.Resolve<IUpdatePropertiesTask>();
            this.devicesPropertiesThread = new Thread(
                () => this.devicesPropertiesTask.RunAsync(
                    this.simulationManagers,
                    this.devicePropertiesActors,
                    this.runningToken.Token));

            try
            {
                this.devicesPropertiesThread.Start();
            }
            catch (Exception e)
            {
                this.log.Error("Unable to start the device properties thread", e);
                throw new Exception("Unable to start the device properties thread", e);
            }
        }

        private void TryToStopReportedPropertiesThread()
        {
            try
            {
                this.devicesPropertiesThread.Interrupt();
            }
            catch (Exception e)
            {
                this.log.Warn("Unable to stop the devices state thread in a clean way", e);
            }
        }
    }
}
