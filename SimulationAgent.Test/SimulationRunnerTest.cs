// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;
using Moq;
using SimulationAgent.Test.helpers;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;
using SimulationModel = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;

namespace SimulationAgent.Test
{
    public class SimulationRunnerTest
    {
        private readonly Mock<IRateLimitingConfig> ratingConfig;
        private readonly Mock<IConcurrencyConfig> concurrencyConfig;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IDeviceModels> deviceModels;
        private readonly Mock<IDeviceModelsGeneration> deviceModelsOverriding;
        private readonly Mock<IDevices> devices;
        private readonly Mock<ISimulations> simulations;
        private readonly Mock<IFactory> factory;
        private readonly Mock<IDeviceStateActor> deviceStateActor;
        private readonly Mock<IDeviceConnectionActor> deviceConnectionActor;
        private readonly Mock<IDeviceTelemetryActor> deviceTelemetryActor;
        private readonly Mock<IDevicePropertiesActor> devicePropertiesActor;
        private readonly Mock<IRateLimiting> rateLimiting;
        private readonly SimulationRunner target;

        public SimulationRunnerTest(ITestOutputHelper log)
        {
            this.ratingConfig = new Mock<IRateLimitingConfig>();
            this.concurrencyConfig = new Mock<IConcurrencyConfig>();
            this.logger = new Mock<ILogger>();
            this.deviceModels = new Mock<IDeviceModels>();
            this.deviceModelsOverriding = new Mock<IDeviceModelsGeneration>();
            this.devices = new Mock<IDevices>();
            this.simulations = new Mock<ISimulations>();
            this.factory = new Mock<IFactory>();
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.deviceConnectionActor = new Mock<IDeviceConnectionActor>();
            this.deviceTelemetryActor = new Mock<IDeviceTelemetryActor>();
            this.devicePropertiesActor = new Mock<IDevicePropertiesActor>();
            this.rateLimiting = new Mock<IRateLimiting>();
            this.ratingConfig.Setup(x => x.DeviceMessagesPerSecond).Returns(10);

            this.target = new SimulationRunner(
                this.ratingConfig.Object,
                this.rateLimiting.Object,
                this.concurrencyConfig.Object,
                this.logger.Object,
                this.deviceModels.Object,
                this.deviceModelsOverriding.Object,
                this.devices.Object,
                this.simulations.Object,
                this.factory.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void TheActiveDevicesCountIsZeroAtStart()
        {
            // Act
            var result = this.target.ActiveDevicesCount;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfActiveDevices()
        {
            // Arrange
            const int ACTIVE_DEVICES_COUNT = 7;

            var simulation = this.GenerateSimulationModel(ACTIVE_DEVICES_COUNT);

            this.SetupSimulationReadyToStart();

            // Act
            this.target.Start(simulation);
            var result = this.target.ActiveDevicesCount;

            // Assert
            Assert.Equal(ACTIVE_DEVICES_COUNT, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void TheNumberOfTotalMessagesCountIsZeroAtStart()
        {
            // Act
            var result = this.target.TotalMessagesCount;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfTotalMessagesCount()
        {
            // Arrange
            const int TOTAL_MESSAGES_PER_DEVICE_COUNT = 10;
            const int ACTIVE_DEVICES_COUNT = 7;

            var simulation = this.GenerateSimulationModel(ACTIVE_DEVICES_COUNT);

            this.SetupSimulationReadyToStart();

            this.deviceTelemetryActor
                .Setup(x => x.TotalMessagesCount)
                .Returns(TOTAL_MESSAGES_PER_DEVICE_COUNT);

            // Act
            this.target.Start(simulation);
            var result = this.target.TotalMessagesCount;

            // Assert
            Assert.Equal(TOTAL_MESSAGES_PER_DEVICE_COUNT * ACTIVE_DEVICES_COUNT, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void TheNumberOfFailedMessagesCountIsZeroAtStart()
        {
            // Act
            var result = this.target.FailedMessagesCount;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfFailedMessagesCount()
        {
            // Arrange
            const int FAILED_MESSAGES_PER_DEVICE_COUNT = 1;
            const int ACTIVE_DEVICES_COUNT = 7;

            var simulation = this.GenerateSimulationModel(ACTIVE_DEVICES_COUNT);

            this.SetupSimulationReadyToStart();

            this.deviceTelemetryActor
                .Setup(x => x.FailedMessagesCount)
                .Returns(FAILED_MESSAGES_PER_DEVICE_COUNT);

            // Act
            this.target.Start(simulation);
            var result = this.target.FailedMessagesCount;

            // Assert
            Assert.Equal(FAILED_MESSAGES_PER_DEVICE_COUNT * ACTIVE_DEVICES_COUNT, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void TheNumberOfFailedDeviceConnectionsCountIsZeroAtStart()
        {
            // Act
            var result = this.target.FailedDeviceConnectionsCount;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfFailedDeviceConnectionsCount()
        {
            // Arrange
            const int FAILED_DEVICE_CONNECTIONS_COUNT = 1;
            const int ACTIVE_DEVICES_COUNT = 7;

            var simulation = this.GenerateSimulationModel(ACTIVE_DEVICES_COUNT);

            this.SetupSimulationReadyToStart();

            this.deviceConnectionActor
                .Setup(x => x.FailedDeviceConnectionsCount)
                .Returns(FAILED_DEVICE_CONNECTIONS_COUNT);

            // Act
            this.target.Start(simulation);
            var result = this.target.FailedDeviceConnectionsCount;

            // Assert
            Assert.Equal(FAILED_DEVICE_CONNECTIONS_COUNT * ACTIVE_DEVICES_COUNT, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void TheNumberOfFailedDeviceTwinUpdatesCountIsZeroAtStart()
        {
            // Act
            var result = this.target.FailedDeviceTwinUpdatesCount;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfFailedDeviceTwinUpdatesCount()
        {
            // Arrange
            const int FAILED_DEVICE_TWIN_UPDATES_COUNT = 1;
            const int ACTIVE_DEVICES_COUNT = 7;

            var simulation = this.GenerateSimulationModel(ACTIVE_DEVICES_COUNT);

            this.SetupSimulationReadyToStart();

            this.devicePropertiesActor
                .Setup(x => x.FailedTwinUpdatesCount)
                .Returns(FAILED_DEVICE_TWIN_UPDATES_COUNT);

            // Act
            this.target.Start(simulation);
            var result = this.target.FailedDeviceTwinUpdatesCount;

            // Assert
            Assert.Equal(FAILED_DEVICE_TWIN_UPDATES_COUNT * ACTIVE_DEVICES_COUNT, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void TheNumberOfSimulationErrorsCountIsZeroAtStart()
        {
            // Act
            var result = this.target.SimulationErrorsCount;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfSimulationErrorsCount()
        {
            // Arrange
            const int FAILED_DEVICE_STATE_COUNT = 1;
            const int FAILED_DEVICE_CONNECTIONS_COUNT = 2;
            const int FAILED_MESSAGES_PER_DEVICE_COUNT = 3;
            const int ACTIVE_DEVICES_COUNT = 7;

            var simulation = this.GenerateSimulationModel(ACTIVE_DEVICES_COUNT);

            this.SetupSimulationReadyToStart();

            this.deviceConnectionActor
                .Setup(x => x.SimulationErrorsCount)
                .Returns(FAILED_DEVICE_CONNECTIONS_COUNT);

            this.deviceStateActor
                .Setup(x => x.SimulationErrorsCount)
                .Returns(FAILED_DEVICE_STATE_COUNT);

            this.deviceTelemetryActor
                .Setup(x => x.FailedMessagesCount)
                .Returns(FAILED_MESSAGES_PER_DEVICE_COUNT);

            // Act
            this.target.Start(simulation);
            var result = this.target.SimulationErrorsCount;

            // Assert
            var EXPECT_RESULT = (FAILED_DEVICE_STATE_COUNT +
                                 FAILED_DEVICE_CONNECTIONS_COUNT +
                                 FAILED_MESSAGES_PER_DEVICE_COUNT) * ACTIVE_DEVICES_COUNT;
            Assert.Equal(EXPECT_RESULT, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItHandlesLoadDeviceModelException()
        {
            // Arrange
            const int ACTIVE_DEVICES_COUNT = 7;

            var simulation = this.GenerateSimulationModel(ACTIVE_DEVICES_COUNT);

            this.deviceModels
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ThrowsAsync(new AggregateException());

            // Act
            var ex = Record.Exception(() => this.target.Start(simulation));

            // Assert
            Assert.Null(ex);
        }

        private SimulationModel GenerateSimulationModel(int activeDevicesCount)
        {
            var models = new List<DeviceModelRef>
            {
                new DeviceModelRef { Id = "01", Count = activeDevicesCount }
            };

            return new SimulationModel
            {
                Id = "1",
                Created = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                Modified = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                ETag = "ETag0",
                Enabled = false,
                DeviceModels = models
            };
        }

        private void SetupSimulationReadyToStart()
        {
            this.SetupSimulations();

            this.SetUpDeviceModelsOverriding();

            this.SetupDevices();

            this.SetupDeviceStateActor();

            this.SetupDeviceConnectionActor();

            this.SetupDeviceTelemetryActor();

            this.SetupDevicePropertiesActor();

            this.SetupActiveDeviceStatus();
        }

        private void SetupActiveDeviceStatus()
        {
            this.deviceStateActor
                .Setup(x => x.IsDeviceActive)
                .Returns(true);
        }

        private void SetupDeviceTelemetryActor()
        {
            this.factory
                .Setup(x => x.Resolve<IDeviceTelemetryActor>())
                .Returns(this.deviceTelemetryActor.Object);
        }

        private void SetupDevicePropertiesActor()
        {
            this.factory
                .Setup(x => x.Resolve<IDevicePropertiesActor>())
                .Returns(this.devicePropertiesActor.Object);
        }

        private void SetupDeviceConnectionActor()
        {
            this.factory
                .Setup(x => x.Resolve<IDeviceConnectionActor>())
                .Returns(this.deviceConnectionActor.Object);
        }

        private void SetupDeviceStateActor()
        {
            this.factory
                .Setup(x => x.Resolve<IDeviceStateActor>())
                .Returns(this.deviceStateActor.Object);
        }

        private void SetupDevices()
        {
            this.devices
                .Setup(x => x.GenerateId(
                    It.IsAny<string>(),
                    It.IsAny<int>()))
                .Returns("Simulate-01");
        }

        private void SetUpDeviceModelsOverriding()
        {
            var telemetry = new List<DeviceModelMessage>();
            var message = new DeviceModelMessage
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            telemetry.Add(message);
            var deviceModel = new DeviceModel { Id = "01", Telemetry = telemetry };

            this.deviceModelsOverriding
                .Setup(x => x.Generate(
                    It.IsAny<DeviceModel>(),
                    It.IsAny<DeviceModelOverride>()))
                .Returns(deviceModel);
        }

        private void SetupSimulations()
        {
            var deviceIds = new List<string> { "01", "02" };

            this.simulations
                .Setup(x => x.GetDeviceIds(It.IsAny<Simulation>()))
                .Returns(deviceIds);
        }
    }
}
