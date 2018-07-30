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

        private readonly ILogger log;

        public DeviceConnectionTask(
            //IRateLimitingConfig ratingConfig,
            IAppConcurrencyConfig appConcurrencyConfig,
            ILogger logger)
        {
            //this.connectionLoopSettings = new ConnectionLoopSettings(ratingConfig);
            this.appConcurrencyConfig = appConcurrencyConfig;
            this.log = logger;
        }

        public async Task RunAsync(
            ConcurrentDictionary<string, ISimulationManager> simulationManagers, 
            ConcurrentDictionary<string, IDeviceConnectionActor> deviceConnectionActors,
            CancellationToken runningToken)
        {
            // Once N devices are attempting to connect, wait until they are done
            var pendingTasksLimit = this.appConcurrencyConfig.MaxPendingConnections;
            var tasks = new List<Task>();

            while (!runningToken.IsCancellationRequested)
            {
                // TODO: resetting counters every few seconds seems to be a bug - to be revisited
                foreach (var manager in simulationManagers)
                {
                    manager.Value.NewConnectionLoop();
                }
                
                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                foreach (var device in deviceConnectionActors)
                {
                    if (device.Value.HasWorkToDo())
                    {
                        tasks.Add(device.Value.RunAsync());
                    }

                    if (tasks.Count < pendingTasksLimit) continue;

                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }

                // If there are pending tasks...
                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Debug("Device state loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, this.appConcurrencyConfig.MinDeviceConnectionLoopDuration);
            }
        }

        private void SlowDownIfTooFast(long duration, int min)
        {
            // Avoid 1msec sleeps
            if (duration >= min || min - duration <= 1) return;

            var pauseMsecs = min - (int) duration;
            this.log.Debug("Pausing", () => new { pauseMsecs });
            Thread.Sleep(pauseMsecs);
        }
    }
}
