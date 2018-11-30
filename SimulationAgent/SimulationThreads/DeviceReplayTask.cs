// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.SimulationThreads
{
    public interface IDeviceReplayTask
    {
        Task RunAsync(
            ConcurrentDictionary<string, ISimulationManager> simulationManagers,
            CancellationToken runningToken
        );
    }

    public class DeviceReplayTask : IDeviceReplayTask
    {
        // Global settings, not affected by hub SKU or simulation settings
        private readonly IAppConcurrencyConfig appConcurrencyConfig;

        private readonly ILogger log;

        public DeviceReplayTask(
            IAppConcurrencyConfig appConcurrencyConfig,
            ILogger logger)
        {
            this.appConcurrencyConfig = appConcurrencyConfig;
            this.log = logger;
        }

        public async Task RunAsync(
            ConcurrentDictionary<string, ISimulationManager> simulationManagers,
            CancellationToken runningToken)
        {
            var tasks = new List<Task>();

            while (!runningToken.IsCancellationRequested)
            {
                // TODO: Do work
                // foreach (var manager in simulationManagers)
                // {
                // }

                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Wait for any pending tasks.
                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Debug("Device-state loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, 1000);
            }
        }

        private void SlowDownIfTooFast(long duration, int min)
        {
            // Avoid sleeping for only one millisecond
            if (duration >= min || min - duration <= 1) return;

            var pauseMsecs = min - (int) duration;
            this.log.Debug("Pausing device-replay thread", () => new { pauseMsecs });
            Thread.Sleep(pauseMsecs);
        }
    }
}
