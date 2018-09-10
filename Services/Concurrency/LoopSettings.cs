// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    // Settings specific to a simulation and the hub SKU in use
    public class ConnectionLoopSettings
    {
        private readonly int registryOperationsPerMinute;
        public double SchedulableFetches { get; set; }
        public double SchedulableRegistrations { get; set; }

        public ConnectionLoopSettings(int registryOperationsPerMinute)
        {
            this.registryOperationsPerMinute = registryOperationsPerMinute;
            this.NewLoop();
        }

        // TODO: this is invoked by a thread, every few seconds. It doesn't seem right
        //       to reset per-minute counters, is it?
        private void NewLoop()
        {
            // Prioritize connections and registrations, so that devices connect as soon as possible
            this.SchedulableFetches = Math.Max(1, this.registryOperationsPerMinute / 25);
            this.SchedulableRegistrations = Math.Max(1, this.registryOperationsPerMinute / 10);
        }
    }

    // Settings specific to a simulation and the hub SKU in use
    public class PropertiesLoopSettings
    {
        private const int SHARED_TWIN_WRITES_ALLOCATION = 2;

        private readonly int twinWritesPerSecond;

        public double SchedulableTaggings { get; set; }

        public PropertiesLoopSettings(int twinWritesPerSecond)
        {
            this.twinWritesPerSecond = twinWritesPerSecond;
            this.NewLoop();
        }

        // TODO: this is invoked by a thread, every few seconds. It doesn't seem right to
        //       reset per-minute counters, is it?
        private void NewLoop()
        {
            // In order for other threads to be able to schedule twin operations,
            // divide by a constant value to prevent the tagging thread from having
            // first priority over twin writes all of the time.
            this.SchedulableTaggings = Math.Max(1, this.twinWritesPerSecond / SHARED_TWIN_WRITES_ALLOCATION);
        }
    }
}