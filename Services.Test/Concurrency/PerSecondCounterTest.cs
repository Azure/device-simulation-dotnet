// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace Services.Test.Concurrency
{
    public class PerSecondCounterTest
    {
        private static ITestOutputHelper log;
        private readonly TargetLogger targetLogger;

        public PerSecondCounterTest(ITestOutputHelper logger)
        {
            log = logger;
            this.targetLogger = new TargetLogger(log);
        }

        /**
         * Calls are slowed down only *after* reaching the limit for events
         * per second. So, when the limit is 60 events/second, 60 events should
         * not be paused.
         */
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntPauseWhenNotNeeded()
        {
            log.WriteLine("Starting test at " + DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff"));

            // Arrange
            const int MAX_SPEED = 60;
            const int EVENTS = MAX_SPEED;
            var target = new PerSecondCounter(MAX_SPEED, "test", this.targetLogger);

            // Act
            var paused = false;
            for (var i = 0; i < EVENTS; i++)
            {
                paused = paused || target.IncreaseAsync(CancellationToken.None).Result;
            }

            // Assert
            Assert.False(paused);
        }

        /**
         * Calls are slowed down only *after* reaching the limit for events
         * per second. So, when the limit is 60 events/second, 60 events should
         * not be paused, the 61st should be paused.
         */
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItPausesWhenNeeded()
        {
            log.WriteLine("Starting test at " + DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff"));

            // Arrange
            const int MAX_SPEED = 60;
            const int EVENTS = MAX_SPEED + 1;
            var target = new PerSecondCounter(MAX_SPEED, "test", this.targetLogger);

            // Act
            var paused = false;
            for (var i = 0; i < EVENTS; i++)
            {
                paused = target.IncreaseAsync(CancellationToken.None).Result;
            }

            // Assert
            Assert.True(paused);
        }

        /**
         * Run 10 events with a limit of 1 event/second.
         * The first call is not paused because nothing happened yet,
         * then 9 events should be paused for ~1 second, for a total time
         * of about 9 seconds. The achieved speed should be ~10/9. For an
         * easier assertion, ignore the first (or last) event and verify that
         * the speed is between 0.9 and 1.0 events/sec.
         * Note: the test is slow on purpose to cover a realistic scenario.
         */
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST), Trait(Constants.SPEED, Constants.SLOW_TEST)]
        public void ItObtainsTheDesiredFrequency_OneEventPerSecond()
        {
            log.WriteLine("Starting test at " + DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff"));

            // Arrange
            const int EVENTS = 10;
            const int MAX_SPEED = 1;
            // When calculating the speed achieved, exclude the events in the last second
            const int EVENTS_TO_IGNORE = 1;
            var target = new PerSecondCounter(MAX_SPEED, "test", this.targetLogger);

            // Act
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (var i = 0; i < EVENTS; i++)
            {
                target.IncreaseAsync(CancellationToken.None).Wait(Constants.TEST_TIMEOUT);
                Thread.Sleep(100);
            }

            // Assert
            var timepassed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;

            double actualSpeed = (double) (EVENTS - EVENTS_TO_IGNORE) * 1000 / timepassed;
            log.WriteLine("Time passed: {0} msecs", timepassed);
            log.WriteLine("Speed: {0} events/sec", actualSpeed);
            Assert.InRange(actualSpeed, MAX_SPEED * 0.9, MAX_SPEED);
        }

        /**
         * Run 41 events with a limit of 20 events/second.
         * The first 20 calls are not paused, then the rating logic
         * starts slowing down the caller. The 41st event falls in the
         * 3rd second which would allow for 60 events. The 41st event is
         * used to force the test to run for at least 2 second, because
         * the events from 21 to 40 will go through as a burst, without pauses.
         * When calculating the speed obtained, ignore the 41st event
         * and verify that the speed is between 19 and 20 events per second.
         */
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItObtainsTheDesiredFrequency_SeveralEventsPerSecond()
        {
            log.WriteLine("Starting test at " + DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff"));

            // Arrange
            const int EVENTS = 41;
            const int MAX_SPEED = 20;
            // TODO: investigate why this is needed, is the rate limiting not working correctly?
            //       https://github.com/Azure/device-simulation-dotnet/issues/127
            const double PRECISION = 0.05; // empiric&acceptable value looking at CI builds

            // When calculating the speed achieved, exclude the events in the last second
            const int EVENTS_TO_IGNORE = 1;

            var target = new PerSecondCounter(MAX_SPEED, "test", this.targetLogger);

            // Act
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var last = now;
            for (var i = 0; i < EVENTS; i++)
            {
                target.IncreaseAsync(CancellationToken.None).Wait(Constants.TEST_TIMEOUT);
                last = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            // Assert
            //long timepassed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
            long timepassed = last - now;
            double actualSpeed = (double) (EVENTS - EVENTS_TO_IGNORE) * 1000 / timepassed;
            log.WriteLine("Time passed: {0} msecs", timepassed);
            log.WriteLine("Speed: {0} events/sec", actualSpeed);
            Assert.InRange(actualSpeed, MAX_SPEED - (1 + PRECISION), MAX_SPEED + PRECISION);
        }

        /**
         * Test similar to "ItDoesntPauseWhenNotNeeded" and "ItObtainsTheDesiredFrequency_SeveralEventsPerSecond"
         * The test runs 40 events in 20 seconds, the first 20 go through as a burst, then
         * the caller is paused for 1 second, then the second half goes through as another
         * burst. The test should take just a bit more than 1 second, definitely less
         * than 2 seconds, so the actual speed should be ~39 events/second.
         */
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItAllowsBurstOfEvents()
        {
            log.WriteLine("Starting test at " + DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff"));

            // Arrange
            const int EVENTS = 40;
            const int MAX_SPEED = EVENTS / 2;

            var target = new PerSecondCounter(MAX_SPEED, "test", this.targetLogger);

            // Act
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (var i = 0; i < EVENTS; i++)
            {
                target.IncreaseAsync(CancellationToken.None).Wait(Constants.TEST_TIMEOUT);
            }

            // Assert
            var timepassed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
            double actualSpeed = (double) EVENTS * 1000 / timepassed;
            log.WriteLine("Time passed: {0} msecs", timepassed);
            log.WriteLine("Speed: {0} events/sec", actualSpeed);
            Assert.InRange(actualSpeed, EVENTS * 0.9, EVENTS);
        }

        /**
         * Another "realistic" scenario, where 1 event happens every 250 msecs
         * as if there was some I/O. Differently then other tests, this
         * avoid bursts on purpose to make sure the internal logic of the
         * rating logic is keeping the internal queue status correct.
         */
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItWorksWhenNoThrottlingIsNeeded()
        {
            // Arrange
            var target = new PerSecondCounter(10, "test", this.targetLogger);

            // Act
            for (var i = 0; i < 10; i++)
            {
                // Assert - there was no pause
                Assert.False(target.IncreaseAsync(CancellationToken.None).Result);
                Task.Delay(250).Wait();
            }
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void FourThreadsTenPerSecondAreThrottledTogether()
        {
            // Arrange
            var events = new ConcurrentBag<DateTimeOffset>();
            var target = new PerSecondCounter(10, "test", this.targetLogger);
            var thread1 = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    target.IncreaseAsync(CancellationToken.None).Wait();
                    events.Add(DateTimeOffset.UtcNow);
                }
            });
            var thread2 = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    target.IncreaseAsync(CancellationToken.None).Wait();
                    events.Add(DateTimeOffset.UtcNow);
                }
            });
            var thread3 = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    target.IncreaseAsync(CancellationToken.None).Wait();
                    events.Add(DateTimeOffset.UtcNow);
                }
            });
            var thread4 = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    target.IncreaseAsync(CancellationToken.None).Wait();
                    events.Add(DateTimeOffset.UtcNow);
                }
            });

            // Act
            while (DateTimeOffset.UtcNow.Millisecond > 200)
            {
                // wait until the next second
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            thread1.Start();
            thread2.Start();
            thread3.Start();
            thread4.Start();
            thread1.Join();
            thread2.Join();
            thread3.Join();
            thread4.Join();

            // Assert
            var passed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
            var j = 0;
            foreach (var e in events.ToImmutableSortedSet())
            {
                j++;
                log.WriteLine(j + ": " + e.ToString("hh:mm:ss.fff"));
            }

            log.WriteLine("time: " + passed);
            Assert.InRange(passed, 3000, 3500);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void FourThreadsTwentyPerSecondAreThrottledTogether()
        {
            // Arrange
            var events = new ConcurrentBag<DateTimeOffset>();
            var target = new PerSecondCounter(20, "test", this.targetLogger);
            var thread1 = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    target.IncreaseAsync(CancellationToken.None).Wait();
                    events.Add(DateTimeOffset.UtcNow);
                }
            });
            var thread2 = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    target.IncreaseAsync(CancellationToken.None).Wait();
                    events.Add(DateTimeOffset.UtcNow);
                }
            });
            var thread3 = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    target.IncreaseAsync(CancellationToken.None).Wait();
                    events.Add(DateTimeOffset.UtcNow);
                }
            });
            var thread4 = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    target.IncreaseAsync(CancellationToken.None).Wait();
                    events.Add(DateTimeOffset.UtcNow);
                }
            });

            // Act
            while (DateTimeOffset.UtcNow.Millisecond > 200)
            {
                // wait until the next second
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            thread1.Start();
            thread2.Start();
            thread3.Start();
            thread4.Start();
            thread1.Join();
            thread2.Join();
            thread3.Join();
            thread4.Join();

            // Assert
            var passed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
            var j = 0;
            foreach (var e in events.ToImmutableSortedSet())
            {
                j++;
                log.WriteLine(j + ": " + e.ToString("hh:mm:ss.fff"));
            }

            log.WriteLine("time: " + passed);
            Assert.InRange(passed, 1000, 1500);
        }

        /**
         * Run two burst separate by a pause of 5 seconds, which is an edge
         * case in the internal implementation, when the queue is cleaned up.
         */
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST), Trait(Constants.SPEED, Constants.SLOW_TEST)]
        public void ItSupportLongPeriodsWithoutEvents()
        {
            log.WriteLine("Starting test at " + DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff"));

            // Arrange
            const int MAX_SPEED = 10;
            const int EVENTS1 = 65;
            const int EVENTS2 = 35;
            var target = new PerSecondCounter(MAX_SPEED, "test", this.targetLogger);

            // Act - Run 2 separate burst, separate by a pause long enough
            // for the internal queue to be cleaned up.
            var t1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (var i = 0; i < EVENTS1; i++)
            {
                target.IncreaseAsync(CancellationToken.None).Wait(Constants.TEST_TIMEOUT);
            }

            var t2 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            Thread.Sleep(5001);

            var t3 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (var i = 0; i < EVENTS2; i++)
            {
                target.IncreaseAsync(CancellationToken.None).Wait(Constants.TEST_TIMEOUT);
            }

            var t4 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Assert
            Assert.InRange(t2 - t1, 6000, 7000);
            Assert.InRange(t4 - t3, 3000, 4000);
        }

        /**
         * This is a long test useful while debugging for manual verifications
         * to check the behavior for a relatively long period.
         * The test should take ~50 seconds to process 1001 events
         * with a limit of 20 events/second.
         */
        //[Fact]
        [Fact(Skip = "Skipping test used only while debugging"), Trait(Constants.TYPE, Constants.UNIT_TEST), Trait(Constants.SPEED, Constants.SLOW_TEST)]
        public void ItObtainsTheDesiredFrequency_DebuggingTest()
        {
            log.WriteLine("Starting test at " + DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff"));

            // Arrange
            const int EVENTS = 1001;
            const int MAX_SPEED = 20;
            // When calculating the speed achieved, exclude the events in the last second
            const int EVENTS_TO_IGNORE = 1;

            var target = new PerSecondCounter(MAX_SPEED, "test", this.targetLogger);

            // Act
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (var i = 0; i < EVENTS; i++)
            {
                target.IncreaseAsync(CancellationToken.None).Wait(Constants.TEST_TIMEOUT);
            }

            // Assert - the test should take ~5 seconds
            var timepassed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
            double actualSpeed = (double) (EVENTS - EVENTS_TO_IGNORE) * 1000 / timepassed;
            log.WriteLine("Time passed: {0} msecs", timepassed);
            log.WriteLine("Speed: {0} events/sec", actualSpeed);
            Assert.InRange(actualSpeed, MAX_SPEED - 1, MAX_SPEED);
        }
    }
}
