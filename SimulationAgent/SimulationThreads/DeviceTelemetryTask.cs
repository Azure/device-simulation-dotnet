// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Linq;
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

        public DeviceTelemetryTask(IAppConcurrencyConfig appConcurrencyConfig, ILogger logger)
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
            var connected = false;

            // Once N devices are attempting to send telemetry, wait until they are done
            // var pendingTaskLimit = this.appConcurrencyConfig.MaxPendingTelemetry;
            // var tasks = new ConcurrentDictionary<int, Task>();

            // var limits = new SemaphoreSlim(pendingTaskLimit, pendingTaskLimit);
            while (!runningToken.IsCancellationRequested)
            {
                // Calculate the first and last actor, which define a range of actors that each thread
                // should *not* send telemetry for. As the number of actors could change at runtime,
                // as different simulations are started, we'll re-calculate these values on each
                // time through this loop.
                int chunkSize = (int)Math.Ceiling(deviceTelemetryActors.Count / (double)threadCount);
                var firstActor = chunkSize * (threadPosition - 1);
                var lastActor = Math.Min(chunkSize * threadPosition, deviceTelemetryActors.Count);
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

                long durationMsecs = 0;
                try
                {
                    var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    var telemetryActors = deviceTelemetryActors.Skip(firstActor).Take(lastActor - firstActor).ToArray();
                    if (!connected && (telemetryActors.Length == 0 || telemetryActors.Length > 0 && telemetryActors.Any(a => !a.Value.DeviceContext.Connected)))
                    {
                        this.log.Info("Devices connecting", () => new { totalDevices = telemetryActors.Length, connected = telemetryActors.Count(a => a.Value.DeviceContext.Connected) });

                        await Task.Delay(10 * 1000);
                        continue;
                    }

                    // If there is no active actors, just set the connected to false
                    if (telemetryActors.Length == 0)
                    {
                        connected = false;
                        continue;
                    }

                    // First time connected, equally split into second to make sure each second has the same load
                    if (!connected)
                    {
                        this.log.Info("All devices connected, schedule telemtry");
                        var groups = telemetryActors.Select(a => a.Value).GroupBy(a => a.Message.Interval);
                        foreach (var group in groups)
                        {
                            var groupActors = group.ToArray();
                            var totalBatch = group.Key.TotalSeconds * 1000 / this.appConcurrencyConfig.MinDeviceTelemetryLoopDuration;
                            var batchCount = (int)Math.Round((double)groupActors.Length / totalBatch);
                            for (var i = 0; i < totalBatch; i++)
                            {
                                var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                                IDeviceTelemetryActor[] batch;
                                if (i == totalBatch - 1)
                                {
                                    // Last batch take all remaining
                                    if (i * batchCount < groupActors.Length)
                                    {
                                        batch = groupActors.Skip(i * batchCount).Take(groupActors.Length - i * batchCount).ToArray();
                                    }
                                    else
                                    {
                                        batch = new IDeviceTelemetryActor[0];
                                    }
                                }
                                else
                                {
                                    batch = groupActors.Skip(i * batchCount).Take(batchCount).ToArray();
                                }

                                Parallel.ForEach(batch, a => a.RunAsync());

                                await SlowDownIfTooFast(
                                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start,
                                    this.appConcurrencyConfig.MinDeviceTelemetryLoopDuration,
                                    runningToken);
                            }
                        }

                        connected = true;
                        continue;
                    }

                    var actors = deviceTelemetryActors.Skip(firstActor).Take(lastActor - firstActor).Where(a => a.Value.HasWorkToDo()).Select(a => a.Value).ToArray();
                    this.log.Info("Telemetry Actions", () => new { actors.Length });
                    Parallel.ForEach(actors, actor =>
                    {
                        actor.RunAsync();
                    });

                    durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                    this.log.Debug("Telemetry loop completed", () => new { durationMsecs });
                }
                catch (Exception e)
                {
                    this.log.Error("Unable to start the device-telemetry threads", e);
                    // this.logDiagnostics.LogServiceError(msg, e);
                    throw new Exception("Unable to start the device-telemetry threads", e);
                }

                await this.SlowDownIfTooFast(durationMsecs, this.appConcurrencyConfig.MinDeviceTelemetryLoopDuration, runningToken);
            }
        }

        private async Task SlowDownIfTooFast(long duration, int min, CancellationToken cancellationToken)
        {
            // Avoid sleeping for only one millisecond
            if (duration >= min || min - duration <= 1) return;

            var pauseMsecs = min - (int)duration;
            this.log.Debug("Pausing device telemetry thread", () => new { pauseMsecs });
            await Task.Delay(pauseMsecs, cancellationToken);
        }
    }
}
