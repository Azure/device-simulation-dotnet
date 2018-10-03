// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.SimulationThreads
{
    public interface IUpdatePropertiesTask
    {
        Task RunAsync(
            ConcurrentDictionary<string, ISimulationManager> simulationManagers,
            ConcurrentDictionary<string, IDevicePropertiesActor> devicePropertiesActors,
            CancellationToken runningToken);
    }

    public class UpdatePropertiesTask : IUpdatePropertiesTask
    {
        // Global settings, not affected by hub SKU or simulation settings
        private readonly ISimulationConcurrencyConfig appConcurrencyConfig;

        private readonly ILogger log;

        public UpdatePropertiesTask(
            //IRateLimitingConfig ratingConfig,
            ISimulationConcurrencyConfig appConcurrencyConfig,
            ILogger logger)
        {
            //this.propertiesLoopSettings = new PropertiesLoopSettings(ratingConfig);
            this.appConcurrencyConfig = appConcurrencyConfig;
            this.log = logger;
        }

        public async Task RunAsync(
            ConcurrentDictionary<string, ISimulationManager> simulationManagers,
            ConcurrentDictionary<string, IDevicePropertiesActor> devicePropertiesActors,
            CancellationToken runningToken)
        {
            // Once N devices are attempting to write twins, wait until they are done
            var pendingTasksLimit = this.appConcurrencyConfig.MaxPendingTwinWrites;
            var tasks = new List<Task>();

            while (!runningToken.IsCancellationRequested)
            {
                // TODO: resetting counters every few seconds seems to be a bug - to be revisited
                // foreach (var manager in simulationManagers)
                // {
                //     manager.Value.NewPropertiesLoop();
                // }

                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (var device in devicePropertiesActors)
                {
                    if (device.Value.HasWorkToDo())
                    {
                        tasks.Add(device.Value.RunAsync());
                    }

                    if (tasks.Count < pendingTasksLimit) continue;

                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Debug("Device-properties loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, this.appConcurrencyConfig.MinDevicePropertiesLoopDuration);
            }

            // If there are pending tasks...
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
                tasks.Clear();
            }
        }

        private void SlowDownIfTooFast(long duration, int min)
        {
            // Avoid sleeping for only one millisecond
            if (duration >= min || min - duration <= 1) return;

            var pauseMsecs = min - (int)duration;
            this.log.Debug("Pausing device-properties thread", () => new { pauseMsecs });
            Thread.Sleep(pauseMsecs);
        }
    }
}
