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
        private readonly Mock<IRateLimiting> rateReporter;
        private readonly StatusController target;

        public StatusControllerTest(ITestOutputHelper log)
        {
            this.preprovisionedIotHub = new Mock<IPreprovisionedIotHub>();
            this.storage = new Mock<IStorageAdapterClient>();
            this.simulations = new Mock<ISimulations>();
            this.logger = new Mock<ILogger>();
            this.servicesConfig = new Mock<IServicesConfig>();
            this.rateReporter = new Mock<IRateLimiting>();

            this.target = new StatusController(
                this.preprovisionedIotHub.Object,
                this.storage.Object,
                this.simulations.Object,
                this.logger.Object,
                this.servicesConfig.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheStatusOfRunningSimulation()
        {
            // Arrange
            this.SetupSimulationForRunner();

            // Act
            var result = this.target.GetAsync().CompleteOrTimeout().Result;

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
            var result = this.target.GetAsync().CompleteOrTimeout().Result;

            // Assert
            Assert.Equal("true", result.Properties["PreprovisionedIoTHub"]);
        }

        private void SetupSimulationForRunner()
        {
            var simulation = new SimulationModel
            {
                Id = SIMULATION_ID,
                Name = "Test Simulation",
                Created = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                Modified = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                ETag = "ETag0",
                Enabled = true,
                PartitioningComplete = true,
                DevicesCreationComplete = true
            };

            var simulations = new List<SimulationModel> { simulation };

            this.simulations
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(simulations);
        }

        private void SetupPreprovisionedIoTHub()
        {
            const string FAKE_KEY = "fakekey";
            const string FAKE_HOST = "hub-1";
            const string IOTHUB_CONNECTION_STRING =
                "hostname=" + FAKE_HOST + ";sharedaccesskeyname=hubowner;sharedaccesskey=" + FAKE_KEY;

            this.servicesConfig
                .Setup(x => x.IoTHubConnString)
                .Returns(IOTHUB_CONNECTION_STRING);
        }
    }
}
