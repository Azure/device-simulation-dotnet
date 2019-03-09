// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            CancellationToken runningToken,
            ISimulationAgentEventHandler simulationAgentEventHandler);
    }

    public class DeviceConnectionTask : IDeviceConnectionTask
    {
        // Global settings, not affected by hub SKU or simulation settings
        private readonly IAppConcurrencyConfig appConcurrencyConfig;

        private readonly ILogger log;

        public DeviceConnectionTask(
            IAppConcurrencyConfig appConcurrencyConfig,
            ILogger logger)
        {
            this.appConcurrencyConfig = appConcurrencyConfig;
            this.log = logger;
        }

        public async Task RunAsync(
            ConcurrentDictionary<string, ISimulationManager> simulationManagers,
            ConcurrentDictionary<string, IDeviceConnectionActor> deviceConnectionActors,
            CancellationToken runningToken,
            ISimulationAgentEventHandler simulationAgentEventHandler)
        {
            try
            {
                // Once N devices are attempting to connect, wait until they are done
                var pendingTasksLimit = this.appConcurrencyConfig.MaxPendingConnections;
                var batchCount = pendingTasksLimit % 100 > 0 ? pendingTasksLimit / 100 + 1 : pendingTasksLimit / 100;

                while (!runningToken.IsCancellationRequested)
                {
                    long durationMsecs = 0;

                    // TODO: resetting counters every few seconds seems to be a bug - to be revisited
                    // Was this introduced to react to the changing number of nodes?
                    //foreach (var manager in simulationManagers)
                    //{
                    //    manager.Value.NewConnectionLoop();
                    //}

                    var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var limits = new SemaphoreSlim(batchCount, batchCount);
                    var actorGroups = deviceConnectionActors.Where(a => a.Value.HasWorkToDo()).Select(a => a.Value).GroupBy(a => a.Status).ToArray();
                    foreach (var group in actorGroups)
                    {
                        var tasks = new List<Task>();

                        var actors = group.ToArray();
                        for (var i = 0; i < (actors.Length % 100 > 0 ? actors.Length / 100 + 1 : actors.Length / 100); i++)
                        {
                            await limits.WaitAsync();
                            var batchActors = actors.Skip(i * 100).Take(100).ToArray();
                            var task = Task.Run(async () =>
                            {
                                await Task.WhenAll(batchActors.Select(actor => actor.RunAsync()));
                                limits.Release();
                            });

                            tasks.Add(task);
                        }

                        // Wait for any pending tasks.
                        if (tasks.Count > 0)
                        {
                            await Task.WhenAll(tasks);
                            tasks.Clear();
                        }
                    }

                    durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                    this.log.Debug("Device-state loop completed", () => new { durationMsecs });

                    await this.SlowDownIfTooFast(durationMsecs, this.appConcurrencyConfig.MinDeviceConnectionLoopDuration, runningToken);
                }
            }
            catch (Exception e)
            {
                var msg = "Device-connection failed";
                this.log.Error(msg, e);
                // this.logDiagnostics.LogServiceError(msg, e);
                simulationAgentEventHandler?.OnError(e);
                // throw new Exception("Unable to start the device-connection thread", e);
            }
        }

        private async Task SlowDownIfTooFast(long duration, int min, CancellationToken runningToken)
        {
            // Avoid sleeping for only one millisecond
            if (duration >= min || min - duration <= 1) return;

            var pauseMsecs = min - (int)duration;
            this.log.Debug("Pausing device-connection thread", () => new { pauseMsecs });
            await Task.Delay(pauseMsecs, runningToken);
        }
    }
}
