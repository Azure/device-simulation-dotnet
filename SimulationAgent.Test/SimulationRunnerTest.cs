// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;
using Moq;
using SimulationAgent.Test.helpers;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;
using SimulationModel = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;
using SimulationRunner = Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.SimulationRunner;

namespace SimulationAgent.Test
{
    public class SimulationRunnerTest
    {
        private readonly Mock<IRateLimitingConfig> ratingConfig;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IDeviceModels> deviceModels;
        private readonly Mock<IDeviceModelsGeneration> deviceModelsOverriding;
        private readonly Mock<IDevices> devices;
        private readonly Mock<ISimulations> simulations;
        private readonly Mock<IFactory> factory;
        private readonly Mock<IDictionary<string, IDeviceStateActor>> deviceStateActors;
        private readonly Mock<IDeviceStateActor> deviceStateActor;
        private readonly Mock<IDeviceConnectionActor> deviceConnectionActor;
        private readonly Mock<IDeviceTelemetryActor> deviceTelemetryActor;
        private readonly Mock<IDevicePropertiesActor> devicePropertiesActor;
        private readonly Mock<UpdateDeviceState> updateDeviceStateLogic;
        private readonly SimulationRunner target;

        public SimulationRunnerTest(ITestOutputHelper log)
        {
            this.ratingConfig = new Mock<IRateLimitingConfig>();
            this.logger = new Mock<ILogger>();
            this.deviceModels = new Mock<IDeviceModels>();
            this.deviceModelsOverriding = new Mock<IDeviceModelsGeneration>();
            this.devices = new Mock<IDevices>();
            this.simulations = new Mock<ISimulations>();
            this.factory = new Mock<IFactory>();
            this.deviceStateActors = new Mock<IDictionary<string, IDeviceStateActor>>();
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.deviceConnectionActor = new Mock<IDeviceConnectionActor>();
            this.deviceTelemetryActor = new Mock<IDeviceTelemetryActor>();
            this.devicePropertiesActor = new Mock<IDevicePropertiesActor>();
            this.updateDeviceStateLogic = new Mock<UpdateDeviceState>();

            this.target = new SimulationRunner(
                this.ratingConfig.Object,
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
            var result = this.target.GetActiveDevicesCount();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfActiveDevices()
        {
            // Arrange
            const int ACTIVE_DEVICES_COUNT = 7;
            var models = new List<DeviceModelRef>
            {
                new DeviceModelRef { Id = "01", Count = ACTIVE_DEVICES_COUNT }
            };

            var simulation = new SimulationModel
            {
                Id = "1",
                Created = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                Modified = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                ETag = "ETag0",
                Enabled = false,
                Version = 1,
                DeviceModels = models
            };

            SetupSimulationReadyToStart();

            // Act
            this.target.Start(simulation);
            var result = this.target.GetActiveDevicesCount();

            // Assert
            Assert.Equal(ACTIVE_DEVICES_COUNT, result);
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

        private void SetupDevicePropertiesActor()
        {
            this.factory
                .Setup(x => x.Resolve<IDevicePropertiesActor>())
                .Returns(this.devicePropertiesActor.Object);
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
            var deviceModel = new DeviceModel { Id = "01" };

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
