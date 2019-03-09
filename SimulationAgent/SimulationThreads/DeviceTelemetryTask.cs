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
            CancellationToken runningToken,
            ISimulationAgentEventHandler simulationAgentEventHandler);
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

        public async Task RunAsync(
            ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors,
            CancellationToken runningToken,
            ISimulationAgentEventHandler simulationAgentEventHandler)
        {
            // TODO: what will happen if we lost the lock on the partition and add a new / old partition back again?
            var connected = false;

            try
            {
                // Once N devices are attempting to send telemetry, wait until they are done
                // var pendingTaskLimit = this.appConcurrencyConfig.MaxPendingTelemetry;
                // var tasks = new ConcurrentDictionary<int, Task>();

                // var limits = new SemaphoreSlim(pendingTaskLimit, pendingTaskLimit);
                while (!runningToken.IsCancellationRequested)
                {
                    if (!connected && (deviceTelemetryActors.Count == 0 || deviceTelemetryActors.Count > 0 && deviceTelemetryActors.Any(a => !a.Value.DeviceContext.Connected)))
                    {
                        this.log.Info("Devices connecting", () => new { totalDevices = deviceTelemetryActors.Count, connected = deviceTelemetryActors.Count(a => a.Value.DeviceContext.Connected) });

                        await Task.Delay(10 * 1000);
                        continue;
                    }

                    // If there is no active actors, just set the connected to false
                    if (!deviceTelemetryActors.Any())
                    {
                        connected = false;
                        continue;
                    }

                    // First time connected, equally split into time range to make sure each second has the same load
                    if (!connected)
                    {
                        this.log.Info("All devices connected, schedule telemtry");
                        var groups = deviceTelemetryActors.Select(a => a.Value).GroupBy(a => a.Message.Interval);
                        foreach (var group in groups)
                        {
                            var groupActors = group.ToArray();
                            var totalBatch = group.Key.TotalSeconds * 1000 / this.appConcurrencyConfig.MinDeviceTelemetryLoopDuration;
                            var batchCount = (int)Math.Round((double)groupActors.Length / totalBatch);
                            for (var i = 0; i < totalBatch; i++)
                            {
                                long durationMsecs = 0;
                                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                                IDeviceTelemetryActor[] batch;
                                if (i == totalBatch - 1) // Last batch take all remaining
                                {
                                    batch = i * batchCount < groupActors.Length ? groupActors.Skip(i * batchCount).Take(groupActors.Length - i * batchCount).ToArray() : Array.Empty<IDeviceTelemetryActor>();
                                }
                                else
                                {
                                    batch = groupActors.Skip(i * batchCount).Take(batchCount).ToArray();
                                }

                                Parallel.ForEach(batch, a => a.RunAsync());

                                durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                                await SlowDownIfTooFast(durationMsecs, this.appConcurrencyConfig.MinDeviceTelemetryLoopDuration, runningToken);
                            }
                        }

                        connected = true;
                    }
                    else
                    {
                        long durationMsecs = 0;
                        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        var actors = deviceTelemetryActors.Where(a => a.Value.HasWorkToDo()).Select(a => a.Value).ToArray();
                        Parallel.ForEach(actors, actor => actor.RunAsync());
                        durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                        this.log.Info("Telemetry loop completed", () => new { durationMsecs, actors.Length });

                        await this.SlowDownIfTooFast(durationMsecs, this.appConcurrencyConfig.MinDeviceTelemetryLoopDuration, runningToken);
                    }
                }
            }
            catch (Exception e)
            {
                this.log.Error("Unable to start the device-telemetry threads", e);
                simulationAgentEventHandler?.OnError(e);
                // this.logDiagnostics.LogServiceError(msg, e);
                // throw new Exception("Unable to start the device-telemetry threads", e);
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
