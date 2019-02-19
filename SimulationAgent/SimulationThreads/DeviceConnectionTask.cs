// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.SimulationThreads
{
    public interface IDeviceConnectionTask
    {
        Task RunAsync(
            ConcurrentDictionary<string, ISimulationManager> simulationManagers,
            ConcurrentDictionary<string, IDeviceConnectionActor> deviceConnectionActors,
            CancellationToken runningToken);
    }

    public class DeviceConnectionTask : IDeviceConnectionTask
    {
        // Global settings, not affected by hub SKU or simulation settings
        private readonly IAppConcurrencyConfig appConcurrencyConfig;
        private readonly IApplicationInsightsLogger aiLogger;
        private readonly ILogger log;

        public DeviceConnectionTask(
            IAppConcurrencyConfig appConcurrencyConfig,
            ILogger logger,
            IApplicationInsightsLogger aiLogger)
        {
            this.appConcurrencyConfig = appConcurrencyConfig;
            this.log = logger;
            this.aiLogger = aiLogger;
        }

        public async Task RunAsync(
            ConcurrentDictionary<string, ISimulationManager> simulationManagers,
            ConcurrentDictionary<string, IDeviceConnectionActor> deviceConnectionActors,
            CancellationToken runningToken)
        {
            // Once N devices are attempting to connect, wait until they are done
            var pendingTasksLimit = this.appConcurrencyConfig.MaxPendingConnections;
            var tasks = new List<Task>();

            // Initialize the Application Insights logger
            this.aiLogger.Init();

            // Keep track of the simulation ID. For now, assume that there is only one
            // simulation running per node.
            // TODO: extend this for the multiple-concurrent-simulation case
            string simulationId = "";

            while (!runningToken.IsCancellationRequested)
            {
                // TODO: resetting counters every few seconds seems to be a bug - to be revisited
                // Was this introduced to react to the changing number of nodes?
                foreach (var manager in simulationManagers)
                {
                    manager.Value.NewConnectionLoop();

                    // TODO: support tracking multiple simulations for the multiple-concurrent-simulation
                    // case.
                    simulationId = manager.Key;
                }

                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (var device in deviceConnectionActors)
                {
                    // Avoid enqueueing async tasks that don't have anything to do
                    if (device.Value.HasWorkToDo())
                    {
                        tasks.Add(device.Value.RunAsync());
                    }

                    if (tasks.Count < pendingTasksLimit) continue;

                    // Log the count of pending connection tasks
                    this.aiLogger.WaitingForConnectionTasks(simulationId, tasks.Count);

                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }

                // Wait for any pending tasks.
                if (tasks.Count > 0)
                {
                    // Log the count of pending connection tasks
                    this.aiLogger.WaitingForConnectionTasks(simulationId, tasks.Count);
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Debug("Device-connection loop completed", () => new { durationMsecs });
                if (durationMsecs > 0)
                {
                    this.aiLogger.DeviceConnectionLoopCompleted(simulationId, durationMsecs);
                }
                this.SlowDownIfTooFast(durationMsecs, this.appConcurrencyConfig.MinDeviceConnectionLoopDuration);
            }
        }

        private void SlowDownIfTooFast(long duration, int min)
        {
            // Avoid sleeping for only one millisecond
            if (duration >= min || min - duration <= 1) return;

            var pauseMsecs = min - (int) duration;
            this.log.Debug("Pausing device-connection thread", () => new { pauseMsecs });
            Thread.Sleep(pauseMsecs);
        }
    }
}
