// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.SimulationThreads
{
    public interface IDeviceStateTask
    {
        Task RunAsync(
            ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors,
            CancellationToken runningToken,
            ISimulationAgentEventHandler simulationAgentEventHandler);
    }

    public class DeviceStateTask : IDeviceStateTask
    {
        private readonly ILogger log;

        // Global settings, not affected by IoT Hub SKU or simulation settings
        private readonly IAppConcurrencyConfig appConcurrencyConfig;

        public DeviceStateTask(
            IAppConcurrencyConfig appConcurrencyConfig,
            ILogger logger)
        {
            this.appConcurrencyConfig = appConcurrencyConfig;
            this.log = logger;
        }

        public async Task RunAsync(
            ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors,
            CancellationToken runningToken, 
            ISimulationAgentEventHandler simulationAgentEventHandler)
        {
            try
            {
                while (!runningToken.IsCancellationRequested)
                {
                    long durationMsecs = 0;

                    var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    // TODO: change to run parallel? 
                    foreach (var deviceStateActor in deviceStateActors)
                    {
                        deviceStateActor.Value.Run();
                    }

                    durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                    this.log.Debug("Device state loop completed", () => new { durationMsecs });

                    await this.SlowDownIfTooFast(durationMsecs, this.appConcurrencyConfig.MinDeviceStateLoopDuration, runningToken);
                }
            }
            catch (Exception e)
            {
                var msg = "Exception happened for device-state task";
                this.log.Error(msg, e);
                simulationAgentEventHandler?.OnError(e);
                // this.logDiagnostics.LogServiceError(msg, e);
                // throw new Exception("Unable to start the device-state thread", e);
            }
        }

        private async Task SlowDownIfTooFast(long duration, int min, CancellationToken runningToken)
        {
            // Avoid sleeping for only one millisecond
            if (duration >= min || min - duration <= 1) return;

            var pauseMsecs = min - (int)duration;
            this.log.Debug("Pausing device state thread", () => new { pauseMsecs });
            await Task.Delay(pauseMsecs, runningToken);
        }
    }
}
