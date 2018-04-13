// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace Services.Test.Concurrency
{
    public class PerMinuteCounterTest
    {
        private static ITestOutputHelper log;

        private readonly TargetLogger targetLogger;

        public PerMinuteCounterTest(ITestOutputHelper logger)
        {
            log = logger;
            this.targetLogger = new TargetLogger(logger);
        }

        /**
         * Calls are slowed down only *after* reaching the limit for events
         * per minute. So, when the limit is 60 events/minute, 60 events should
         * not be paused.
         */
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntPauseWhenNotNeeded()
        {
            log.WriteLine("Starting test at " + DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff"));

            // Arrange - The number of calls doesn't exceed the max frequency
            const int FREQUENCY = 60;
            const int CALLS = FREQUENCY;
            var target = new PerMinuteCounter(FREQUENCY, "test", this.targetLogger);

            // Act
            var paused = false;
            for (int i = 0; i < CALLS; i++)
            {
                paused = paused || target.IncreaseAsync(CancellationToken.None).Result;
            }

            // Assert - The counter never throttled the call
            Assert.False(paused);
        }

        /**
         * This test is equivalent to PerSecondCounterTest.ItPausesWhenNeeded
         * so it should not be needed. It's here only for manual tests while debugging.
         * The test takes about 1 minute, so it is disabled by default.
         */
        //[Fact]
        [Fact(Skip="Skipping test used only while debugging"), Trait(Constants.TYPE, Constants.UNIT_TEST), Trait(Constants.SPEED, Constants.SLOW_TEST)]
        public void ItPausesWhenNeeded_DebuggingTest()
        {
            log.WriteLine("Starting test at " + DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff"));

            // Arrange - The number of calls exceeds the max frequency by one
            const int FREQUENCY = 60;
            const int CALLS = FREQUENCY + 1;
            var target = new PerMinuteCounter(FREQUENCY, "test", this.targetLogger);

            // Act
            var pauses = 0;
            for (int i = 0; i < CALLS; i++)
            {
                pauses += target.IncreaseAsync(CancellationToken.None).Result ? 1 : 0;
            }

            // Assert - The counter throttled the call once
            Assert.Equal(1, pauses);
        }
    }
}
