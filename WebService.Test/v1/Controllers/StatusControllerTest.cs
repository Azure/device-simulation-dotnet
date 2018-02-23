using System;
using System.Collections.Generic;
using StatusController = Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers.StatusController;
using SimulationModel = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;
using Xunit;
using Xunit.Abstractions;
using WebService.Test.helpers;
using Moq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

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

            this.target = new StatusController(
                this.preprovisionedIotHub.Object,
                this.storage.Object,
                this.simulations.Object,
                this.logger.Object,
                this.servicesConfig.Object,
                this.deploymentConfig.Object,
                this.connectionStringManager.Object,
                this.simulationRunner.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task GetTest()
        {
            // Arrange
            SetupSimulationForRunner();

            this.simulationRunner
                .Setup(x => x.GetActiveDeviceCount())
                .Returns(5);

            // Act
            var result = await this.target.Get();

            // Assert
            Assert.Equal(3, result.Properties.Count);
            Assert.Equal("true", result.Properties["SimulationRunning"]);
            Assert.Equal("5", result.Properties["ActiveDeviceCount"]);

        }

        private void SetupSimulationForRunner()
        {
            var simulation = new SimulationModel
            {
                Id = SIMULATION_ID,
                Created = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                Modified = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                ETag = "ETag0",
                Enabled = true,
                Version = 1
            };

            var simulations = new List<SimulationModel>
            {
                simulation
            };

            this.simulations
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(simulations);
        }
    }
}
