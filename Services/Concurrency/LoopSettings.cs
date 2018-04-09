﻿// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
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
        
        public ConnectionLoopSettings(IRateLimitingConfig ratingConfig)
        {
            this.ratingConfig = ratingConfig;
            this.NewLoop();
        }

        public void NewLoop()
        {
            // Prioritize connections and registrations, so that devices connect as soon as possible
            this.SchedulableFetches = Math.Max(1, this.ratingConfig.RegistryOperationsPerMinute / 25);
            this.SchedulableRegistrations = Math.Max(1, this.ratingConfig.RegistryOperationsPerMinute / 10);
        }
    }

    public class PropertiesLoopSettings
    {
        private const int SHARED_TWIN_WRITES_ALLOCATION = 2;

        public const int MIN_LOOP_DURATION = 500;

        private readonly IRateLimitingConfig ratingConfig;

        public double SchedulableTaggings { get; set; }

        public PropertiesLoopSettings(IRateLimitingConfig ratingConfig)
        {
            this.ratingConfig = ratingConfig;
            this.NewLoop();
        }

        public void NewLoop()
        {
            // In order for other threads to be able to schedule twin opertations,
            // divide by a constant value to prevent the tagging thread from having
            // first priority over twin writes all of the time.
            this.SchedulableTaggings = Math.Max(1, this.ratingConfig.TwinWritesPerSecond / SHARED_TWIN_WRITES_ALLOCATION);
        }
    }
}