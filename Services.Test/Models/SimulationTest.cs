// Copyright (c) Microsoft. All rights reserved.

using System;
using Services.Test.helpers;
using Xunit;
using SimulationModel = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;

namespace Services.Test.Models
{
    public class SimulationTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReportsIfItIsActive()
        {
            // Arrange
            var enabledButEnded = new SimulationModel
            {
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(-1)
            };

            var currentButDisabled = new SimulationModel
            {
                Enabled = false,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(+1)
            };

            var currentAndEnabled = new SimulationModel
            {
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(+1)
            };

            // Assert
            Assert.False(enabledButEnded.IsActiveNow);
            Assert.False(currentButDisabled.IsActiveNow);
            Assert.True(currentAndEnabled.IsActiveNow);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReportsIfPartitioningIsRequired()
        {
            // Arrange
            var activeAndPartitioned = new SimulationModel
            {
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                PartitioningComplete = true
            };

            var activeAndNotPartitioned = new SimulationModel
            {
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                PartitioningComplete = false
            };
            var notActiveAndNotPartitioned = new SimulationModel
            {
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(-1),
                PartitioningComplete = false
            };

            // Assert
            Assert.False(activeAndPartitioned.PartitioningRequired);
            Assert.True(activeAndNotPartitioned.PartitioningRequired);
            Assert.False(notActiveAndNotPartitioned.PartitioningRequired);
        }
    }
}
