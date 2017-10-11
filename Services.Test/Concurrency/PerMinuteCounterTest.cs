// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.Concurrency
{
    public class PerMinuteCounterTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntPauseWhenNotNeeded()
        {
            // Arrange
            const int FREQUENCY = 60;
            const int CALLS = FREQUENCY;
            var target = new PerMinuteCounter(FREQUENCY);

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
            var target = new PerMinuteCounter(FREQUENCY);

            // Act
            var paused = false;
            for (int i = 0; i < CALLS; i++)
            {
                paused = paused || target.RateAsync().Result;
            }

            // Assert
            Assert.True(paused);
        }
    }
}
