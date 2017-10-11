// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace Services.Test.Concurrency
{
    public class PerSecondCounterTest
    {
        private static ITestOutputHelper log;
        private const int TEST_TIMEOUT = 5000;

        public PerSecondCounterTest(ITestOutputHelper logger)
        {
            log = logger;
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntPauseWhenNotNeeded()
        {
            // Arrange
            const int FREQUENCY = 60;
            const int CALLS = FREQUENCY;
            var target = new PerSecondCounter(FREQUENCY);

            // Act
            var paused = false;
            for (int i = 0; i < CALLS; i++)
            {
                paused = paused || target.RateAsync().Result;
            }

            // Assert
            Assert.False(paused);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItPausesWhenNeeded()
        {
            // Arrange
            const int FREQUENCY = 60;
            const int CALLS = FREQUENCY + 1;
            var target = new PerSecondCounter(FREQUENCY);

            // Act
            var paused = false;
            for (int i = 0; i < CALLS; i++)
            {
                paused = paused || target.RateAsync().Result;
            }

            // Assert
            Assert.True(paused);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItObtainsTheDesiredFrequency()
        {
            // Arrange
            const int LOOPS = 10;
            const int FREQ = 1;
            var target = new PerSecondCounter(FREQ);

            // Act - Loop 10 times
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (int i = 0; i < LOOPS; i++)
            {
                target.RateAsync().Wait(TEST_TIMEOUT);
                Thread.Sleep(100);
            }

            // Assert - the test should take ~10 seconds
            var timepassed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
            var frequency = LOOPS * 1000 / timepassed;
            log.WriteLine("Time passed: {0} msecs", timepassed);
            log.WriteLine("Frequency: {0}", frequency);
            Assert.True(frequency >= FREQ - 1, "The test was too slow: " + frequency + " >= " + (FREQ - 1));
            Assert.True(frequency <= FREQ + 1, "The test took too fast: " + frequency + " <= " + (FREQ + 1));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItObtainsTheDesiredFrequency2()
        {
            // Arrange
            const int LOOPS = 60;
            const int FREQ = 10;
            var target = new PerSecondCounter(FREQ);

            // Act - Loop 10 times
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (int i = 0; i < LOOPS; i++)
            {
                target.RateAsync().Wait(TEST_TIMEOUT);
            }

            // Assert - the test should take ~5 seconds
            var timepassed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
            var frequency = LOOPS * 1000 / timepassed;
            log.WriteLine("Time passed: {0} msecs", timepassed);
            log.WriteLine("Frequency: {0}", frequency);
            Assert.True(frequency >= FREQ - 1, "The test was too slow: " + frequency + " >= " + (FREQ - 1));
            Assert.True(frequency <= FREQ + 1, "The test took too fast: " + frequency + " <= " + (FREQ + 1));
        }
    }
}
