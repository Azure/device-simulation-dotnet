using Moq;
using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Xunit.Abstractions;
using SimulationRunner = Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.SimulationRunner;
using SimulationModel = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;
using Xunit;
using SimulationAgent.Test.helpers;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using System.Collections.Concurrent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;

namespace SimulationAgent.Test
{
    public enum ActorStatus
    {
        None,
        Updating
    }
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
            var result = this.target.GetActiveDeviceCount();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void AbleToGetActiveDeviceCount()
        {
            // Arrange
            var models = new List<DeviceModelRef>
            {
                new DeviceModelRef { Id = "01", Count = 7 }
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
            var result = this.target.GetActiveDeviceCount();

            // Assert
            Assert.Equal(7, result);
        }

        private void SetupSimulationReadyToStart()
        {
            var deviceIds = new List<string> { "01", "02" };

            this.simulations
                .Setup(x => x.GetDeviceIds(It.IsAny<Simulation>()))
                .Returns(deviceIds);

            var deviceModel = new DeviceModel { Id = "01" };

            this.deviceModelsOverriding
                .Setup(x => x.Generate(
                    It.IsAny<DeviceModel>(),
                    It.IsAny<DeviceModelOverride>()))
                .Returns(deviceModel);

            this.devices
                .Setup(x => x.GenerateId(
                    It.IsAny<string>(),
                    It.IsAny<int>()))
                .Returns("Simulate-01");

            this.deviceStateActor
                .Setup(x => x.IsDeviceActive)
                .Returns(true);

            this.factory
                .Setup(x => x.Resolve<IDeviceStateActor>())
                .Returns(this.deviceStateActor.Object);

            this.factory
                .Setup(x => x.Resolve<IDeviceConnectionActor>())
                .Returns(this.deviceConnectionActor.Object);

            this.factory
                .Setup(x => x.Resolve<IDeviceTelemetryActor>())
                .Returns(this.deviceTelemetryActor.Object);
}
    }
}
