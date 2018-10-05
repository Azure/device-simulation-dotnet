// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.SimulationThreads
{
    public interface IDeviceStateTask
    {
        void Run(
            ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors,
            CancellationToken runningToken);
    }

    public class DeviceStateTask : IDeviceStateTask
    {
        private readonly ILogger log;

        // Global settings, not affected by IoT Hub SKU or simulation settings
        private readonly ISimulationConcurrencyConfig simulationConcurrencyConfig;

        public DeviceStateTask(
            ISimulationConcurrencyConfig simulationConcurrencyConfig,
            ILogger logger)
        {
            this.simulationConcurrencyConfig = simulationConcurrencyConfig;
            this.log = logger;
        }

        public void Run(
            ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors,
            CancellationToken runningToken)
        {
            do
            {
                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (var deviceStateActor in deviceStateActors)
                {
                    deviceStateActor.Value.Run();
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Debug("Device state loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, this.simulationConcurrencyConfig.MinDeviceStateLoopDuration);
            } while (!runningToken.IsCancellationRequested);
        }

        private void SlowDownIfTooFast(long duration, int min)
        {
            // Avoid sleeping for only one millisecond
            if (duration >= min || min - duration <= 1) return;

            var pauseMsecs = min - (int)duration;
            this.log.Debug("Pausing device state thread", () => new { pauseMsecs });
            Thread.Sleep(pauseMsecs);
        }
    }
}
