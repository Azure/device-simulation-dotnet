// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Commons;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

// TODO: optimize the memory usage https://github.com/Azure/device-simulation-dotnet/issues/80
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public enum CounterUnit
    {
        Second,
        Minute
    }

    public class PerSecondCounter : RatedCounter
    {
        public PerSecondCounter(int rate, string name, ILogger logger)
            : base(rate, CounterUnit.Second, name, logger)
        {
        }
    }

    public class PerMinuteCounter : RatedCounter
    {
        public PerMinuteCounter(int rate, string name, ILogger logger)
            : base(rate, CounterUnit.Minute, name, logger)
        {
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

        // Counter unit
        private readonly CounterUnit counterUnit;

        // Counter rate
        private readonly int rate;

        // The max frequency to enforce, e.g. the maximum number
        // of events allowed within a minute, a second, etc.
        private int eventsPerTimeUnit;

        // A description used for diagnostics logs
        private readonly string name;

        private readonly ILogger log;

        private readonly IDictionary<DateTimeOffset, int> timeStamps;

        private long[] counters;

        private volatile int headIndex = 0;
        private volatile int tailIndex = 0;
        private const int INITIAL_SIZE = 32;
        private int mask = INITIAL_SIZE - 1;

        private long lastKey = 0;

        private readonly object foreignLock = new object();

        public RatedCounter(int rate, CounterUnit counterUnit, string name, ILogger logger)
        {
            this.log = logger;

            switch (counterUnit)
            {
                case CounterUnit.Minute:
                    this.timeUnitLength = 60 * 1000;
                    break;
                case CounterUnit.Second:
                    this.timeUnitLength = 1000;
                    break;
            }

            if (rate < MIN_RATE)
            {
                var msg = "The counter rate value must be greater than or equal to " + MIN_RATE;
                this.log.Error(msg, () => new { name, rate, counterUnit = counterUnit.ToString() });
                throw new InvalidConfigurationException(msg);
            }

            if (timeUnitLength < MIN_TIME_UNIT)
            {
                var msg = "The counter time unit value must be greater than or equal to " + MIN_TIME_UNIT;
                this.log.Error(msg, () => new { name, rate, counterUnit = counterUnit.ToString() });
                throw new InvalidConfigurationException(msg);
            }

            this.rate = rate;
            this.name = name;
            this.eventsPerTimeUnit = this.rate;
            this.counterUnit = counterUnit;

            this.timeStamps = new ConcurrentDictionary<DateTimeOffset, int>();
            this.counters = new long[INITIAL_SIZE];

            this.log.Debug("New counter", () => new { name, this.rate, timeUnitLength });

            ScheduledTasksExecutor.ScheduleTask(Clean, counterUnit == CounterUnit.Second ? 10 * 1000 : 60 * 10 * 1000);
        }

        private void CreateNew(DateTimeOffset key)
        {
            int tail = tailIndex;
            if (tail < headIndex + mask)
            {
                this.counters[tail & mask] = 1;
                this.timeStamps[key] = tail & mask;
                tailIndex = tail + 1;
            }
            else
            {
                int head = headIndex;
                int count = tailIndex - headIndex;
                if (count >= mask)
                {
                    long[] newArray = new long[this.counters.Length << 1];
                    var dictionary = new Dictionary<int, int>(this.counters.Length);
                    for (int i = 0; i < this.counters.Length; i++)
                    {
                        newArray[i] = this.counters[(i + head) & mask];
                        dictionary[(i + head) & mask] = i;
                    }
                    this.counters = newArray;
                    foreach (var item in this.timeStamps)
                    {
                        this.timeStamps[item.Key] = dictionary[item.Value];
                    }

                    // Reset the field values, incl. the mask.
                    headIndex = 0;
                    tailIndex = tail = count;
                    mask = (mask << 1) | 1;
                }

                this.counters[tail & mask] = 1;
                this.timeStamps[key] = tail & mask;
                tailIndex = tail + 1;
            }

            Interlocked.Exchange(ref lastKey, key.ToUnixTimeSeconds());
        }

        /// <summary>
        /// First get the key for current timestamp
        ///       get the key for last item in dictionary
        /// If the key in dictionary is older than current timestamp key (means current slot is not used), 
        ///     create a new one and return 0
        /// Else
        ///     Read the value for last slot
        ///     If it's less than limit, 
        ///         incremental one
        ///     Else
        ///         Create a new one
        /// </summary>
        /// <returns></returns>
        public long GetPause()
        {
            var now = DateTimeOffset.UtcNow;
            var key = DateTimeOffset.Parse(now.ToString("yyyy-MM-dd HH:mm:ss") + " z");
            if (this.counterUnit == CounterUnit.Minute)
            {
                key = DateTimeOffset.Parse(now.ToString("yyyy-MM-dd HH:mm") + " z");
            }

            var lastCounterKey = DateTimeOffset.FromUnixTimeSeconds(Interlocked.Read(ref this.lastKey));
            if (lastCounterKey == DateTimeOffset.FromUnixTimeSeconds(0))
            {
                lock (this.foreignLock)
                {
                    lastCounterKey = DateTimeOffset.FromUnixTimeSeconds(Interlocked.Read(ref this.lastKey));
                    if (lastCounterKey == DateTimeOffset.FromUnixTimeSeconds(0))
                    {
                        CreateNew(key);
                        return 0;
                    }
                }
            }

            if (key > lastCounterKey)
            {
                lock (this.foreignLock)
                {
                    lastCounterKey = DateTimeOffset.FromUnixTimeSeconds(Interlocked.Read(ref this.lastKey));
                    if (key > lastCounterKey)
                    {
                        CreateNew(key);
                        return 0;
                    }
                }
            }

            // Console.WriteLine("Last Counter: {0}", lastCounterKey);
            var currentValue = Interlocked.Read(ref this.counters[this.timeStamps[lastCounterKey]]);
            // Console.WriteLine("CurrentValue: {0}", currentValue);
            if (currentValue < this.eventsPerTimeUnit)
            {
                var newValue = Interlocked.Increment(ref this.counters[this.timeStamps[lastCounterKey]]);
                if (newValue <= this.eventsPerTimeUnit)
                {
                    var pause = (long)(lastCounterKey - now).TotalMilliseconds;
                    return pause > 0 ? pause : 0;
                }
                else
                {
                    Interlocked.Decrement(ref this.counters[this.timeStamps[lastCounterKey]]);
                }
            }

            lock (this.foreignLock)
            {
                var newLastCounterKey = DateTimeOffset.FromUnixTimeSeconds(Interlocked.Read(ref this.lastKey));
                // Console.WriteLine("New Last Counter: {0}", newLastCounter.Key);
                if (newLastCounterKey == lastCounterKey)
                {
                    var newKey = this.counterUnit == CounterUnit.Minute ? lastCounterKey.AddMinutes(1) : lastCounterKey.AddSeconds(1);
                    CreateNew(newKey);
                    var pause = (long)(newKey - now).TotalMilliseconds;
                    return pause > 0 ? pause : 0;
                }
            }

            return this.GetPause();
        }

        // Increase the counter, taking a pause if the caller is going too fast.
        // Return a boolean indicating whether a pause was required.
        public async Task<bool> IncreaseAsync(CancellationToken token)
        {
            var pauseMsecs = this.GetPause();

            if (pauseMsecs > 0)
            {
                await Task.Delay((int)pauseMsecs, token);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get messages throughput.
        /// </summary>
        /// <returns>Throughput: messages per second</returns>
        public double GetThroughputForMessages()
        {
            double speed = 0;

            lock (foreignLock)
            {
                if (this.timeStamps.Count <= 1)
                {
                    return speed;
                }

                var values = this.timeStamps.Values.ToList();
                values.RemoveAt(values.Count - 1);

                switch (this.counterUnit)
                {
                    case CounterUnit.Minute:
                        speed = (double)values.Sum(v => this.counters[v]) / values.Count / 60;
                        break;
                    case CounterUnit.Second:
                        speed = (double)values.Sum(v => this.counters[v]) / values.Count;
                        break;
                }
            }

            return speed;
        }

        public void ResetCounter()
        {
            lock (this.foreignLock)
            {
                this.timeStamps.Clear();
                this.tailIndex = 0;
                this.headIndex = 0;
                this.mask = INITIAL_SIZE - 1;
                this.lastKey = 0;
                this.counters = new long[mask];
            }
        }

        public void ChangeConcurrencyFactor(int factor)
        {
            factor = Math.Max(1, factor);
            this.eventsPerTimeUnit = Convert.ToInt32(Math.Round(this.rate / (double)factor));
        }

        private void LogThroughput()
        {
            if (this.log.LogLevel <= LogLevel.Debug)
            {
                double speed = this.GetThroughputForMessages();
                this.log.Info(this.name, () => new { speed });
            }
        }

        private void Clean()
        {
            var now = DateTimeOffset.UtcNow;
            var key = DateTimeOffset.Parse(now.ToString("yyyy-MM-dd HH:mm:ss") + " z").AddSeconds(-10);
            if (this.counterUnit == CounterUnit.Minute)
            {
                key = DateTimeOffset.Parse(now.ToString("yyyy-MM-dd HH:mm") + " z").AddMinutes(-10);
            }

            var keys = this.timeStamps.Keys.Where(k => k < key).ToArray();

            lock (foreignLock)
            {
                foreach (var k in keys)
                {
                    Interlocked.Exchange(ref this.counters[this.headIndex & mask], 0);
                    this.headIndex++;
                    this.timeStamps.Remove(k);
                }
            }
        }
    }
}
