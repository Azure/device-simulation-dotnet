// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers;
using Moq;
using WebService.Test.helpers;
using Xunit;
using Xunit.Abstractions;
using SimulationModel = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;

namespace WebService.Test.v1.Controllers
{
    public class StatusControllerTest
    {
        private const string SIMULATION_ID = "1";

        private readonly Mock<IPreprovisionedIotHub> preprovisionedIotHub;
        private readonly Mock<IStorageAdapterClient> storage;
        private readonly Mock<ISimulations> simulations;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IServicesConfig> servicesConfig;
        private readonly Mock<IDeploymentConfig> deploymentConfig;
        private readonly Mock<IIotHubConnectionStringManager> connectionStringManager;
        private readonly Mock<ISimulationRunner> simulationRunner;
        private readonly Mock<IRateLimiting> rateReporter;
        private readonly StatusController target;

        public StatusControllerTest(ITestOutputHelper log)
        {
            this.preprovisionedIotHub = new Mock<IPreprovisionedIotHub>();
            this.storage = new Mock<IStorageAdapterClient>();
            this.simulations = new Mock<ISimulations>();
            this.logger = new Mock<ILogger>();
            this.servicesConfig = new Mock<IServicesConfig>();
            this.deploymentConfig = new Mock<IDeploymentConfig>();
            this.connectionStringManager = new Mock<IIotHubConnectionStringManager>();
            this.simulationRunner = new Mock<ISimulationRunner>();
            this.rateReporter = new Mock<IRateLimiting>();

            this.target = new StatusController(
                this.preprovisionedIotHub.Object,
                this.storage.Object,
                this.simulations.Object,
                this.logger.Object,
                this.servicesConfig.Object,
                this.deploymentConfig.Object,
                this.connectionStringManager.Object,
                this.simulationRunner.Object,
                this.rateReporter.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfActiveDevices()
        {
            // Arrange
            this.SetupSimulationForRunner();
            const int ACTIVE_DEVICES_COUNT = 5;
            this.simulationRunner
                .Setup(x => x.ActiveDevicesCount)
                .Returns(ACTIVE_DEVICES_COUNT);

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal(ACTIVE_DEVICES_COUNT.ToString(), result.Properties["ActiveDevicesCount"]);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheThroughputOfMessagesPerSecond()
        {
            // Arrange
            const double MESSAGE_THROUGHPUT = 15.5556;
            this.SetupSimulationForRunner();
            this.rateReporter
                .Setup(x => x.GetThroughputForMessages())
                .Returns(MESSAGE_THROUGHPUT);

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal(MESSAGE_THROUGHPUT.ToString("F"), result.Properties["MessagesPerSecond"]);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfTotalMessages()
        {
            // Arrange
            const int TOTAL_MESSAGES_COUNT = 10;
            this.SetupSimulationForRunner();
            this.simulationRunner
                .Setup(x => x.TotalMessagesCount)
                .Returns(TOTAL_MESSAGES_COUNT);

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal(TOTAL_MESSAGES_COUNT.ToString(), result.Properties["TotalMessagesCount"]);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfFailedMessages()
        {
            // Arrange
            const int FAILED_MESSAGES_COUNT = 5;
            this.SetupSimulationForRunner();
            this.simulationRunner
                .Setup(x => x.FailedMessagesCount)
                .Returns(FAILED_MESSAGES_COUNT);

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal(FAILED_MESSAGES_COUNT.ToString(), result.Properties["FailedMessagesCount"]);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfSimulationErrors()
        {
            // Arrange
            const int SIMULATION_ERRORS_COUNT = 5;
            this.SetupSimulationForRunner();
            this.simulationRunner
                .Setup(x => x.SimulationErrorsCount)
                .Returns(SIMULATION_ERRORS_COUNT);

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal(SIMULATION_ERRORS_COUNT.ToString(), result.Properties["SimulationErrorsCount"]);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheStatusOfRunningSimulation()
        {
            // Arrange
            this.SetupSimulationForRunner();

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal("true", result.Properties["SimulationRunning"]);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheStatusOfPreprovisionedIoTHub()
        {
            // Arrange
            this.SetupSimulationForRunner();
            this.SetupPreprovisionedIoTHub();

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal("true", result.Properties["PreprovisionedIoTHub"]);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheUrlOfPreprovisionedIoTHub()
        {
            // Arrange
            this.SetupSimulationForRunner();
            this.SetupPreprovisionedIoTHub();

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Contains("https://portal.azure.com/", result.Properties["PreprovisionedIoTHubMetricsUrl"]);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsPreprovisionedIoTHubInUse()
        {
            // Arrange
            this.SetupSimulationForRunner();
            this.SetupPreprovisionedIoTHub();

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal("true", result.Properties["PreprovisionedIoTHubInUse"]);
        }

        private void SetupSimulationForRunner()
        {
            var simulation = new SimulationModel
            {
                Id = SIMULATION_ID,
                Created = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                Modified = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                ETag = "ETag0",
                Enabled = true
            };

            var simulations = new List<SimulationModel>
            {
                simulation
            };

            this.simulations
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(simulations);
        }

        private void SetupPreprovisionedIoTHub()
        {
            const string IOTHUB_CONNECTION_STRING = "hostname=hub-1;sharedaccesskeyname=hubowner;sharedaccesskey=fakekey";

            this.servicesConfig
               .Setup(x => x.IoTHubConnString)
               .Returns(IOTHUB_CONNECTION_STRING);

            this.connectionStringManager
                .Setup(x => x.GetIotHubConnectionString())
                .Returns(IOTHUB_CONNECTION_STRING);
        }
    }
}
