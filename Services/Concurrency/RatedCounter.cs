// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public class PerSecondCounter : RatedCounter
    {
        public PerSecondCounter(int rate, string name, ILogger logger)
            : base(rate, 1000, name, logger)
        {
        }
    }

    public class PerMinuteCounter : RatedCounter
    {
        public PerMinuteCounter(int rate, string name, ILogger logger)
            : base(rate, 60 * 1000, name, logger)
        {
        }
    }

    // TODO: optimize the memory usage for this counter (see Queue<long> usage)
    //       https://github.com/Azure/device-simulation-dotnet/issues/80
    public class PerDayCounter : RatedCounter
    {
        public PerDayCounter(int rate, string name, ILogger logger)
            : base(rate, 86400 * 1000, name, logger)
        {
            throw new NotSupportedException("Daily counters are not supported yet due to memory constraints.");
        }
    }

    // Leaky bucket counter
    // Note: all the time values are expressed in milliseconds
    public abstract class RatedCounter
    {
        // At least 1 second duration
        private const double MIN_TIME_UNIT = 1000;

        // At least 1 event per time unit
        private const double MIN_RATE = 1;

        // Milliseconds in the time unit (e.g. 1 minute, 1 second, etc.)
        private readonly double timeUnitLength;

        // The max frequency to enforce, e.g. the maximum number
        // of events allowed within a minute, a second, etc.
        private readonly int eventsPerTimeUnit;

        // A description used for diagnostics logs
        private readonly string name;

        private readonly ILogger log;

        // Timestamp of recent events, to calculate rate
        // Note: currently, the memory usage depends on the length of the period to
        //       monitor, so this is a good place for future optimizations
        private readonly Queue<long> timestamps;

        public RatedCounter(int rate, double timeUnitLength, string name, ILogger logger)
        {
            this.log = logger;

            if (rate < MIN_RATE)
            {
                var msg = "The counter rate value must be greater than or equal to " + MIN_RATE;
                this.log.Error(msg, () => new { name, rate, timeUnitLength });
                throw new InvalidConfigurationException(msg);
            }

            if (timeUnitLength < MIN_TIME_UNIT)
            {
                var msg = "The counter time unit value must be greater than or equal to " + MIN_TIME_UNIT;
                this.log.Error(msg, () => new { name, rate, timeUnitLength });
                throw new InvalidConfigurationException(msg);
            }

            this.name = name;

            this.eventsPerTimeUnit = rate;
            this.timeUnitLength = timeUnitLength;

            this.timestamps = new Queue<long>();

            this.log.Info("New counter", () => new { name, rate, timeUnitLength });
        }

        // Increase the counter, taking a pause if the caller is going too fast.
        // Return a boolean indicating whether a pause was required.
        public async Task<bool> IncreaseAsync(CancellationToken token)
        {
            long pause;

            this.LogThroughput();

            // Note: keep the code fast, e.g. leave ASAP and don't I/O while locking
            // TODO: improve performance: https://github.com/Azure/device-simulation-dotnet/issues/80
            //       * remove O(n) lookups
            //       * optimize memory usage (e.g. daily counters)
            lock (this.timestamps)
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                this.CleanQueue(now);

                // No pause if the limit hasn't been reached yet,
                if (this.timestamps.Count < this.eventsPerTimeUnit)
                {
                    this.timestamps.Enqueue(now);
                    return false;
                }

                long when;
                now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var startFrom = now - this.timeUnitLength;
                var howManyInTheLastTimeUnit = this.timestamps.Count(t => t > startFrom);

                // No pause if the limit hasn't been reached in the last time unit
                if (howManyInTheLastTimeUnit < this.eventsPerTimeUnit)
                {
                    when = Math.Max(this.timestamps.Last(), now);
                }
                else
                {
                    // Add one [time unit] since when the Nth event ran
                    var oneUnitTimeAgo = this.timestamps.ElementAt(this.timestamps.Count - this.eventsPerTimeUnit);
                    when = oneUnitTimeAgo + (long) this.timeUnitLength;
                }

                pause = when - now;

                this.timestamps.Enqueue(when);

                // Ignore short pauses
                if (pause < 1.01)
                {
                    return false;
                }
            }

            // The caller is send too many events, if this happens you
            // should consider redesigning the simulation logic to run
            // slower, rather than relying purely on the counter
            if (pause > 60000)
            {
                this.log.Warn("Pausing for more than a minute",
                    () => new { this.name, seconds = pause / 1000 });
            }
            else if (pause > 5000)
            {
                this.log.Warn("Pausing for several seconds",
                    () => new { this.name, seconds = pause / 1000 });
            }
            else
            {
                this.log.Info("Pausing", () => new { this.name, millisecs = pause });
            }

            await Task.Delay((int) pause, token);

            return true;
        }

        private void LogThroughput()
        {
            if (this.log.LogLevel <= LogLevel.Debug && this.timestamps.Count > 1)
            {
                double speed = 0;
                lock (this.timestamps)
                {
                    long time = this.timestamps.Last() - this.timestamps.First();
                    speed = (int) (1000 * (double) this.timestamps.Count / time * 10) / 10;
                }
                this.log.Info(this.name + "/second", () => new { speed });
            }
        }

        private void CleanQueue(long now)
        {
            // Clean up queue
            while (this.timestamps.Count > 0 && (now - this.timestamps.Peek()) > 2 * this.timeUnitLength)
            {
                this.timestamps.Dequeue();
            }
        }
    }
}
