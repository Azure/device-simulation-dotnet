// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

    public class DeviceTelemetryTask : IDeviceTelemetryTask
    {
        private readonly ILogger log;

        // Global settings, not affected by hub SKU or simulation settings
        private readonly IAppConcurrencyConfig appConcurrencyConfig;

        public DeviceTelemetryTask(
            IAppConcurrencyConfig appConcurrencyConfig,
            ILogger logger)
        {
            this.appConcurrencyConfig = appConcurrencyConfig;
            this.log = logger;
        }

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
        public async Task RunAsync(
            ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors,
            int threadPosition,
            int threadCount,
            CancellationToken runningToken)
        {
            // Once N devices are attempting to send telemetry, wait until they are done
            var pendingTaskLimit = this.appConcurrencyConfig.MaxPendingTelemetry;
            var tasks = new List<Task>();

            while (!runningToken.IsCancellationRequested)
            {
                // Calculate the first and last actor, which define a range of actors that each thread
                // should *not* send telemetry for. As the number of actors could change at runtime,
                // as different simulations are started, we'll re-calculate these values on each
                // time through this loop.
                int chunkSize = (int) Math.Ceiling(deviceTelemetryActors.Count / (double) threadCount);
                var firstActor = chunkSize * (threadPosition - 1);
                var lastActor = Math.Min(chunkSize * threadPosition, deviceTelemetryActors.Count);

                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var index = -1;
                foreach (var telemetry in deviceTelemetryActors)
                {
                    // Only send telemetry for devices *other* than the ones in 
                    // the chunk for the current thread, for example:
                    // 
                    //    Count = 20000
                    //    chunkSize = 6667
                    //
                    //    threadPosition 1:     0,  6667
                    //    threadPosition 2:  6667, 13334
                    //    threadPosition 3: 13334, 20000
                    //
                    //    threadPosition == 1
                    //
                    //    firstActor   lastActor
                    //    |            |
                    //    v            v
                    //    +------------+-------------------------+
                    //    |    send    | don't send | don't send | 
                    //    +------------+-------------------------+
                    //
                    //
                    //    threadPosition == 2
                    //
                    //                 firstActor   lastActor
                    //                 |            |
                    //                 v            v
                    //    +------------+-------------------------+
                    //    | don't send |    send    | don't send | 
                    //    +------------+-------------------------+
                    //
                    //
                    //    threadPosition == 3
                    //
                    //                              firstActor   lastActor
                    //                              |            |
                    //                              v            v
                    //    +------------+-------------------------+
                    //    | don't send | don't send |    send    | 
                    //    +------------+-------------------------+
                    index++;
                    if (index >= firstActor && index < lastActor)
                    {
                        tasks.Add(telemetry.Value.RunAsync());

                        if (tasks.Count < pendingTaskLimit)
                            continue;

                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Debug("Telemetry loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, this.appConcurrencyConfig.MinDeviceTelemetryLoopDuration);
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

            var pauseMsecs = min - (int) duration;
            this.log.Debug("Pausing device telemetry thread", () => new { pauseMsecs });
            Thread.Sleep(pauseMsecs);
        }
    }
}
