﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;
using Moq;
using SimulationAgent.Test.helpers;
using System;
using System.Collections.Generic;
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
        public void ActiveCountInitToZero()
        {
            // Act
            var result = this.target.GetActiveDevicesCount();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void AbleToGetActiveDeviceCount()
        {
            // Arrange
            const int DEVICE_COUNT = 7;
            var models = new List<DeviceModelRef>
            {
                new DeviceModelRef { Id = "01", Count = DEVICE_COUNT }
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
            Assert.Equal(DEVICE_COUNT, result);
        }

        private void SetupSimulationReadyToStart()
        {
            var deviceIds = new List<string> { "01", "02" };

            // Setup simulations
            this.simulations
                .Setup(x => x.GetDeviceIds(It.IsAny<Simulation>()))
                .Returns(deviceIds);

            var deviceModel = new DeviceModel { Id = "01" };

            // Setup deviceModel overriding
            this.deviceModelsOverriding
                .Setup(x => x.Generate(
                    It.IsAny<DeviceModel>(),
                    It.IsAny<DeviceModelOverride>()))
                .Returns(deviceModel);

            // Setup devices for simulation runner
            this.devices
                .Setup(x => x.GenerateId(
                    It.IsAny<string>(),
                    It.IsAny<int>()))
                .Returns("Simulate-01");

            // Setup device active status
            this.deviceStateActor
                .Setup(x => x.IsDeviceActive)
                .Returns(true);

            // Setup deviceStateActor
            this.factory
                .Setup(x => x.Resolve<IDeviceStateActor>())
                .Returns(this.deviceStateActor.Object);

            // Setup deviceConnectionActor
            this.factory
                .Setup(x => x.Resolve<IDeviceConnectionActor>())
                .Returns(this.deviceConnectionActor.Object);

            // Setup deviceTelemetryActor
            this.factory
                .Setup(x => x.Resolve<IDeviceTelemetryActor>())
                .Returns(this.deviceTelemetryActor.Object);
        }
    }
}
