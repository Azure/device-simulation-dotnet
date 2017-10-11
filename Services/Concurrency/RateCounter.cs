// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public class PerMinuteCounter : RateCounter
    {
        public PerMinuteCounter(double rate) : base(rate, 60000)
        {
        }
    }

    public class PerSecondCounter : RateCounter
    {
        public PerSecondCounter(double rate) : base(rate, 1000)
        {
        }
    }

    // See "Leaky Bucket" for algorithm details
    public abstract class RateCounter
    {
        // Precision used with real numbers
        private const double ZERO = 0.0001;

        // Milliseconds in the time unit (e.g. 1 minute, 1 second, etc.)
        // All the operations in the class are measured in milliseconds.
        private readonly double timeUnitLength;

        // The max frequency to enforce, e.g. the maximum number
        // of events allowed within a minute, a second, etc.
        private readonly double eventsPerTimeUnit;

        // The maximum number of events allowed within one millisecond
        private readonly double eventsPerMsec;

        // The expected time between 2 events, calculated off the frequency
        // and used to pause the caller when it's going too fast.
        private readonly double eventInterval;

        // Used for concurrency
        private readonly object semaphor;

        // How many events occurred during the last minute
        private double count;

        // The time of the last event
        private long timestamp;

        public RateCounter(double rate, double timeUnitLength)
        {
            this.eventsPerTimeUnit = rate;
            this.timeUnitLength = timeUnitLength;

            // e.g. 100 events/sec in 60000 msecs = 0.001666666667 events per msec
            // e.g.  10 events/sec in 1000 msecs  = 0.01 events per msec
            this.eventsPerMsec = rate / this.timeUnitLength;

            // e.g. 60000 / 100 = 600 msecs = one event every 0.6 secs
            // use this value to decide how long to pause when the client
            // is generating too many events
            this.eventInterval = timeUnitLength / rate;

            this.semaphor = new object();
            this.count = 0;
            this.timestamp = 0;
        }

        public async Task<bool> RateAsync()
        {
            double pause = 0;

            lock (this.semaphor)
            {
                double timePassed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - this.timestamp;

                if (this.count <= ZERO || timePassed >= this.timeUnitLength)
                {
                    // Reset, and add 1 to count the current event
                    this.count = 1;
                }
                else
                {
                    // Reduce the counter by an amount depending on the time passed,
                    // and add 1 to count the current event
                    // E.g. if 6 seconds passed since the last event, and the limit is 100 events/minute,
                    // 6000 * (100 / 60000) = 10 => decrease the count by 10, i.e. the client can generate 10
                    // events in the next minute (the current + 9 more events)
                    this.count = 1 + this.count - timePassed * this.eventsPerMsec;

                    // If the client is generating too many events, pause for an amount of time
                    // depending on the expected frequency
                    if (this.count > this.eventsPerTimeUnit)
                    {
                        pause = (this.count - this.eventsPerTimeUnit) * this.eventInterval;
                        this.count -= pause * this.eventsPerMsec;
                    }
                }
            }

            if (pause > 0)
            {
                await Task.Delay((int) pause);
            }

            // Avoid going backwards
            lock (this.semaphor)
            {
                this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            return pause > 0;
        }
    }
}
