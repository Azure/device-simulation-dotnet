// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.SimulationThreads
{
    public interface IDeviceTelemetryTask
    {
        Task RunAsync(
            ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors,
            int threadPosition,
            int threadCount,
            CancellationToken runningToken);
    }

    public class DeviceTelemetryTask: IDeviceTelemetryTask
    {
        private readonly ILogger log;

        // Global settings, not affected by hub SKU or simulation settings
        private readonly ISimulationConcurrencyConfig simulationConcurrencyConfig;

        public DeviceTelemetryTask(
            ISimulationConcurrencyConfig simulationConcurrencyConfig,
            ILogger logger)
        {
            this.simulationConcurrencyConfig = simulationConcurrencyConfig;
            this.log = logger;
        }

        public async Task RunAsync(
            ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors,
            int threadPosition,
            int threadCount,
            CancellationToken runningToken)
        {
            /**
             * Examples:
             *    threadCount = 3
             *
             *    Count = 20000
             *    chunkSize = 6667
             *    threadPosition 1:     0,  6667
             *    threadPosition 2:  6667, 13334
             *    threadPosition 3: 13334, 20000
             *
             *    Count = 11
             *    chunkSize = 4
             *    threadPosition 1: 0,  4
             *    threadPosition 2: 4,  8
             *    threadPosition 3: 8, 11
             */
            int chunkSize = (int) Math.Ceiling(deviceTelemetryActors.Count / (double) threadCount);
            var firstDevice = chunkSize * (threadPosition - 1);
            var lastDevice = Math.Min(chunkSize * threadPosition, deviceTelemetryActors.Count);

            // Once N devices are attempting to send telemetry, wait until they are done
            var pendingTaskLimit = this.simulationConcurrencyConfig.MaxPendingTelemetry;
            var tasks = new List<Task>();

            while (!runningToken.IsCancellationRequested)
            {
                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var pos = 0;
                foreach (var telemetry in deviceTelemetryActors)
                {
                    // Work only on a subset of all devices
                    if (!(pos >= firstDevice && pos < lastDevice))
                    {
                        tasks.Add(telemetry.Value.RunAsync());
                        if (tasks.Count < pendingTaskLimit) continue;

                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }

                    pos++;
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Debug("Telemetry loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, this.simulationConcurrencyConfig.MinDeviceTelemetryLoopDuration);
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
            this.log.Debug("Pausing device telemetry thread", () => new { pauseMsecs });
            Thread.Sleep(pauseMsecs);
        }
    }
}
