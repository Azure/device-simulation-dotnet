// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            CancellationToken runningToken,
            ISimulationAgentEventHandler simulationAgentEventHandler);
    }

    public class UpdatePropertiesTask : IUpdatePropertiesTask
    {
        // Global settings, not affected by hub SKU or simulation settings
        private readonly IAppConcurrencyConfig appConcurrencyConfig;

        private readonly ILogger log;

        public UpdatePropertiesTask(
            IAppConcurrencyConfig appConcurrencyConfig,
            ILogger logger)
        {
            this.appConcurrencyConfig = appConcurrencyConfig;
            this.log = logger;
        }

        public async Task RunAsync(
            ConcurrentDictionary<string, ISimulationManager> simulationManagers,
            ConcurrentDictionary<string, IDevicePropertiesActor> devicePropertiesActors,
            CancellationToken runningToken,
            ISimulationAgentEventHandler simulationAgentEventHandler)
        {
            // Once N devices are attempting to write twins, wait until they are done
            var pendingTasksLimit = this.appConcurrencyConfig.MaxPendingTwinWrites;

            try
            {

                while (!runningToken.IsCancellationRequested)
                {
                    long durationMsecs = 0;

                    // TODO: resetting counters every few seconds seems to be a bug - to be revisited
                    foreach (var manager in simulationManagers)
                    {
                        manager.Value.NewPropertiesLoop();
                    }

                    var tasks = new List<Task>();

                    var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var limits = new SemaphoreSlim(pendingTasksLimit, pendingTasksLimit);
                    var actors = devicePropertiesActors.Where(a => a.Value.HasWorkToDo()).ToArray();
                    foreach (var actor in actors)
                    {
                        await limits.WaitAsync();
                        var task = actor.Value.RunAsync();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler
                        task.ContinueWith(t =>
                        {
                            limits.Release();
                        });
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        tasks.Add(task);
                    }

                    if (tasks.Count > 0)
                    {
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }

                    durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                    this.log.Debug("Device-properties loop completed", () => new { durationMsecs, actors.Length });

                    await this.SlowDownIfTooFast(durationMsecs, this.appConcurrencyConfig.MinDevicePropertiesLoopDuration, runningToken);
                }
            }
            catch (Exception e)
            {
                var msg = "Device-properties task failed";
                this.log.Error(msg, e);
                // this.logDiagnostics.LogServiceError(msg, e);
                // throw new Exception("Device-properties task failed", e);
                simulationAgentEventHandler?.OnError(e);
            }
        }

        private async Task SlowDownIfTooFast(long duration, int min, CancellationToken runningToken)
        {
            // Avoid sleeping for only one millisecond
            if (duration >= min || min - duration <= 1) return;

            var pauseMsecs = min - (int)duration;
            this.log.Debug("Pausing device-properties thread", () => new { pauseMsecs });
            await Task.Delay(pauseMsecs, runningToken);
        }
    }
}
