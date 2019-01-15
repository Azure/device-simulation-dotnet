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

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReportsIfItShouldBeRunning()
        {
            // Arrange
            var shouldBeRunning = new SimulationModel
            {
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                PartitioningComplete = true,
                DevicesCreationComplete = true
            };
            var notRunningPartitioningIncomplete = new SimulationModel
            {
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                PartitioningComplete = false,
                DevicesCreationComplete = true
            };
            var notRunningCreationIncomplete = new SimulationModel
            {
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                PartitioningComplete = true,
                DevicesCreationComplete = false
            };
            var notRunningNotActive = new SimulationModel
            {
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(-1),
                PartitioningComplete = true,
                DevicesCreationComplete = true
            };

            // Assert
            Assert.True(shouldBeRunning.ShouldBeRunning);
            Assert.False(notRunningPartitioningIncomplete.ShouldBeRunning);
            Assert.False(notRunningCreationIncomplete.ShouldBeRunning);
            Assert.False(notRunningNotActive.ShouldBeRunning);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReportsIfDevicesShouldBeCreated()
        {
            // Arrange
            var shouldCreate1 = new SimulationModel
            {
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                PartitioningComplete = true,
                DevicesCreationComplete = false
            };
            var shouldCreate2 = new SimulationModel
            {
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                PartitioningComplete = false,
                DevicesCreationComplete = false
            };
            var shouldNotCreateNotActive = new SimulationModel
            {
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(-1),
                PartitioningComplete = true,
                DevicesCreationComplete = false
            };

            // Assert
            Assert.True(shouldCreate1.DeviceCreationRequired);
            Assert.True(shouldCreate2.DeviceCreationRequired);
            Assert.False(shouldNotCreateNotActive.DeviceCreationRequired);
        }

        [Theory, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        [InlineData(true, true, true, true, false)]
        [InlineData(true, true, true, false, false)]
        [InlineData(true, true, false, true, false)]
        [InlineData(true, false, true, true, false)]
        [InlineData(true, true, false, false, false)]
        [InlineData(true, false, true, false, false)]
        [InlineData(true, false, false, true, false)]
        [InlineData(true, false, false, false, false)]
        [InlineData(false, true, true, true, true)]
        [InlineData(false, true, true, false, true)]
        [InlineData(false, true, false, true, false)]
        [InlineData(false, false, true, true, true)]
        [InlineData(false, true, false, false, false)]
        [InlineData(false, false, true, false, false)]
        [InlineData(false, false, false, true, false)]
        [InlineData(false, false, false, false, false)]
        public void CheckIfDevicesDeletionRequiredIsPopulatedCorrect(
            bool enabled, 
            bool deleteDevicesOnce, 
            bool devicesCreationComplete, 
            bool deleteDevicesWhenSimulationEnds, 
            bool expected)
        {
            // Arrange and Act
            var target = new SimulationModel
            {
                Enabled = enabled,
                DeleteDevicesOnce = deleteDevicesOnce,
                DevicesCreationComplete = devicesCreationComplete,
                DeleteDevicesWhenSimulationEnds = deleteDevicesWhenSimulationEnds
            };

            //Assert
            Assert.Equal(target.DeviceDeletionRequired, expected);
        }
    }
}
