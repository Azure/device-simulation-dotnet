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

        // Global settings, not affected by hub SKU or simulation settings
        private readonly IAppConcurrencyConfig appConcurrencyConfig;

        public DeviceStateTask(
            IAppConcurrencyConfig appConcurrencyConfig,
            ILogger logger)
        {
            this.appConcurrencyConfig = appConcurrencyConfig;
            this.log = logger;
        }

        public void Run(
            ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors,
            CancellationToken runningToken)
        {
            while (!runningToken.IsCancellationRequested)
            {
                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (var device in deviceStateActors)
                {
                    device.Value.Run();
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Debug("Device state loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, this.appConcurrencyConfig.MinDeviceStateLoopDuration);
            }
        }

        private void SlowDownIfTooFast(long duration, int min)
        {
            // Avoid 1msec sleeps
            if (duration >= min || min - duration <= 1) return;

            var pauseMsecs = min - (int) duration;
            this.log.Debug("Pausing device state thread", () => new { pauseMsecs });
            Thread.Sleep(pauseMsecs);
        }
    }
}
