// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers;
using Moq;
using WebService.Test.helpers;
using Xunit;
using Xunit.Abstractions;

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
        private readonly Mock<IConnectionStrings> connectionStrings;
        private readonly Mock<IRateLimiting> rateReporter;
        private readonly Mock<IStatusService> statusService;
        private readonly Mock<IConfig> config;

        private readonly StatusController target;

        public StatusControllerTest(ITestOutputHelper log)
        {

            this.preprovisionedIotHub = new Mock<IPreprovisionedIotHub>();
            this.storage = new Mock<IStorageAdapterClient>();
            this.simulations = new Mock<ISimulations>();
            this.logger = new Mock<ILogger>();
            this.servicesConfig = new Mock<IServicesConfig>();
            this.connectionStrings = new Mock<IConnectionStrings>();
            this.rateReporter = new Mock<IRateLimiting>();

            this.statusService = new Mock<IStatusService>();
            this.config = new Mock<IConfig>();

            this.target = new StatusController(this.config.Object, this.statusService.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsHealthOfSimulationService()
        {
            //Arrange
            this.statusService
                .Setup(x => x.GetStatusAsync())
                .ReturnsAsync(new StatusServiceModel(true, "Alive and well!"));

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

            this.connectionStrings
                .Setup(x => x.GetAsync())
                .ReturnsAsync(IOTHUB_CONNECTION_STRING);

            Assert.True(result.Status.IsHealthy);

        }
    }
}
