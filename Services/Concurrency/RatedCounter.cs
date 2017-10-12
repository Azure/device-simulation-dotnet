﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public class PerSecondCounter : RatedCounter
    {
        public PerSecondCounter(double rate, string name, ILogger logger)
            : base(rate, 1000, name, logger)
        {
        }
    }

    public class PerMinuteCounter : RatedCounter
    {
        public PerMinuteCounter(double rate, string name, ILogger logger)
            : base(rate, 60 * 1000, name, logger)
        {
        }
    }

    // TODO: optimize the memory usage for this counter
    public class PerDayCounter : RatedCounter
    {
        public PerDayCounter(double rate, string name, ILogger logger)
            : base(rate, 86400 * 1000, name, logger)
        {
        }
    }

    // Leaky bucket counter
    public abstract class RatedCounter
    {
        // At least 1 second duration
        private const double MIN_TIME_UNIT = 1000;

        // At least 1 event per time unit
        private const double MIN_RATE = 1;

        // Milliseconds in the time unit (e.g. 1 minute, 1 second, etc.)
        // All the operations in the class are measured in milliseconds.
        private readonly double timeUnitLength;

        // The max frequency to enforce, e.g. the maximum number
        // of events allowed within a minute, a second, etc.
        private readonly double eventsPerTimeUnit;

        // The expected time between 2 events, calculated off the frequency
        // and used to pause the caller when it's going too fast.
        private readonly double eventInterval;

        // A description used for diagnostics logs
        private readonly string name;

        private readonly ILogger log;

        // Timestamp of recent events, to calculate rate
        // Note: currently, the memory usage depends on the length of the period to
        //       monitor, so this is a good place for future optimizations
        private readonly Queue<long> timestamps;

        // Keep track of the last element enqueued, to avoid O(n) lookups
        private long lastTimestamp;

        public RatedCounter(double rate, double timeUnitLength, string name, ILogger logger)
        {
            if (rate < MIN_RATE)
            {
                var msg = "The counter rate value must be greater or equal to " + MIN_RATE;
                this.log.Error(msg, () => new { name, rate, timeUnitLength });
                throw new InvalidConfigurationException(msg);
            }

            if (timeUnitLength < MIN_TIME_UNIT)
            {
                var msg = "The counter time unit value must be greater or equal to " + MIN_TIME_UNIT;
                this.log.Error(msg, () => new { name, rate, timeUnitLength });
                throw new InvalidConfigurationException(msg);
            }

            this.name = name;
            this.log = logger;

            this.eventsPerTimeUnit = rate;
            this.timeUnitLength = timeUnitLength;

            // e.g. 60000 / 100 = 600 msecs = one event every 0.6 secs
            this.eventInterval = timeUnitLength / rate;

            this.timestamps = new Queue<long>();
            this.lastTimestamp = 0;

            this.log.Debug("New counter", () => new { name, rate, timeUnitLength });
        }

        public async Task<bool> IncreaseAsync()
        {
            long pause = 0;

            // Note: keep the code fast, e.g. leave ASAP and don't I/O while locking
            lock (this.timestamps)
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Remove old events from the queue
                var oneTimeUnitAgo = now - this.timeUnitLength;
                while (this.timestamps.Count > 0 && this.timestamps.Peek() <= oneTimeUnitAgo)
                {
                    this.timestamps.Dequeue();
                }

                // How many events happened in the last time unit, and how many
                // will happen (the queue can contain future timestamps)
                var count = this.timestamps.Count;
                if (count < this.eventsPerTimeUnit)
                {
                    this.lastTimestamp = now;
                    this.timestamps.Enqueue(now);
                    return false;
                }

                // Calculate when the next event is allowed to run, i.e. how long to pause
                var nextTime = (long) (this.lastTimestamp + this.timeUnitLength + this.eventInterval * (count - this.eventsPerTimeUnit));
                pause = nextTime - now;

                this.lastTimestamp = nextTime;
                this.timestamps.Enqueue(nextTime);
            }

            this.log.Debug("Pausing", () => new { this.name, millisecs = pause });
            if (pause > 5000)
            {
                if (pause > 60000)
                {
                    this.log.Error("The pause will last more than a minute",
                        () => new { this.name, seconds = pause / 1000 });
                }
                else
                {
                    this.log.Warn("The pause will last several seconds",
                        () => new { this.name, seconds = pause / 1000 });
                }
            }

            await Task.Delay((int) pause);

            return true;
        }
    }
}