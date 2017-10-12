// Copyright (c) Microsoft. All rights reserved.

using System;
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
        private const int TEST_TIMEOUT = 5000;

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
                paused = paused || target.RateAsync().Result;
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
                paused = target.RateAsync().Result;
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
                target.RateAsync().Wait(TEST_TIMEOUT);
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
         * the events from 21 to 40 will go through as a burst, without pausing.
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
            // When calculating the speed achieved, exclude the events in the last second
            const int EVENTS_TO_IGNORE = 1;

            var target = new PerSecondCounter(MAX_SPEED, "test", this.targetLogger);

            // Act
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (var i = 0; i < EVENTS; i++)
            {
                target.RateAsync().Wait(TEST_TIMEOUT);
            }

            // Assert
            var timepassed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
            double actualSpeed = (double) (EVENTS - EVENTS_TO_IGNORE) * 1000 / timepassed;
            log.WriteLine("Time passed: {0} msecs", timepassed);
            log.WriteLine("Speed: {0} events/sec", actualSpeed);
            Assert.InRange(actualSpeed, MAX_SPEED - 1, MAX_SPEED);
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
                target.RateAsync().Wait(TEST_TIMEOUT);
            }

            // Assert
            var timepassed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
            double actualSpeed = (double) EVENTS * 1000 / timepassed;
            log.WriteLine("Time passed: {0} msecs", timepassed);
            log.WriteLine("Speed: {0} events/sec", actualSpeed);
            Assert.InRange(actualSpeed, EVENTS * 0.9, EVENTS);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItWorksWhenNoThrottlingIsNeeded()
        {
            // Arrange
            var target = new PerSecondCounter(10, "test", this.targetLogger);

            // Act
            for (int i = 0; i < 10; i++)
            {
                target.RateAsync().Wait(TEST_TIMEOUT);
                Task.Delay(250).Wait();
            }

            // Assert

        }

        /**
         * This is a long test useful while debugging for manual verifications
         * to check the behavior for a relatively long period.
         * The test should take ~50 seconds to process 1001 events
         * with a limit of 20 events/second.
         */
        //[Fact]
        [Fact(Skip="Test used only while debugging"), Trait(Constants.TYPE, Constants.UNIT_TEST), Trait(Constants.SPEED, Constants.SLOW_TEST)]
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
                target.RateAsync().Wait(TEST_TIMEOUT);
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
