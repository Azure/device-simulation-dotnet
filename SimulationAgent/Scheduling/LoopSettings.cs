// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Scheduling
{
    public class StateLoopSettings
    {
        public const int MIN_LOOP_DURATION = 999;
    }

    public class TelemetryLoopSettings
    {
        public const int MIN_LOOP_DURATION = 500;
    }

    public class ConnectionLoopSettings
    {
        public const int MIN_LOOP_DURATION = 600;

        private readonly IRateLimitingConfig ratingConfig;

        public double SchedulableFetches { get; set; }
        public double SchedulableRegistrations { get; set; }
        public double SchedulableTaggings { get; set; }

        public ConnectionLoopSettings(IRateLimitingConfig ratingConfig)
        {
            this.ratingConfig = ratingConfig;
            this.NewLoop();
        }

        public void NewLoop()
        {
            // Prioritize connections and registrations, so that devices connect as soon as possible
            this.SchedulableFetches = Math.Max(1, this.ratingConfig.RegistryOperationsPerMinute / 30);
            this.SchedulableRegistrations = Math.Max(1, this.ratingConfig.RegistryOperationsPerMinute / 10);
            this.SchedulableTaggings = Math.Max(1, this.ratingConfig.RegistryOperationsPerMinute / 30);
        }
    }
}
