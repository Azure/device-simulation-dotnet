// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.AzureManagementAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel;
using Moq;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Controllers
{
    public class SimulationsControllerTest
    {
        public SimulationsControllerTest()
        {
            this.simulationsService = new Mock<ISimulations>();
            this.servicesConfig = new Mock<IServicesConfig>();
            this.deploymentConfig = new Mock<IDeploymentConfig>();
            this.connectionStringManager = new Mock<IIotHubConnectionStringManager>();
            this.iothubMetrics = new Mock<IIothubMetrics>();
            this.preprovisionedIotHub = new Mock<IPreprovisionedIotHub>();
            this.simulationAgent = new Mock<ISimulationAgent>();
            this.rateReporter = new Mock<IRateLimiting>();
            this.log = new Mock<ILogger>();

            this.target = new SimulationsController(
                this.simulationsService.Object,
                this.servicesConfig.Object,
                this.deploymentConfig.Object,
                this.connectionStringManager.Object,
                this.iothubMetrics.Object,
                this.preprovisionedIotHub.Object,
                this.simulationAgent.Object,
                this.rateReporter.Object,
                this.log.Object);
        }

        private readonly Mock<ISimulations> simulationsService;
        private readonly Mock<IServicesConfig> servicesConfig;
        private readonly Mock<IDeploymentConfig> deploymentConfig;
        private readonly Mock<IIotHubConnectionStringManager> connectionStringManager;
        private readonly Mock<IIothubMetrics> iothubMetrics;
        private readonly Mock<IPreprovisionedIotHub> preprovisionedIotHub;
        private readonly Mock<ISimulationAgent> simulationAgent;
        private readonly Mock<IRateLimiting> rateReporter;
        private readonly Mock<ILogger> log;
        private readonly SimulationsController target;

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesSimulationWithValidInput()
        {
            // Arrange
            const string ID = "1";
            var simulation = this.GetSimulationById(ID);

            this.simulationsService
                .Setup(x => x.InsertAsync(It.IsAny<Simulation>(), It.IsAny<string>()))
                .ReturnsAsync(simulation);

            // Act
            var simulationApiModel = SimulationApiModel.FromServiceModel(simulation);

            var postAsyncTask = this.target.PostAsync(simulationApiModel);
            postAsyncTask.Wait(Constants.TEST_TIMEOUT);
            var result = postAsyncTask.Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(simulation.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesTheDefaultSimulation()
        {
            // Arrange
            const string DEFAULT_SIMULATION_ID = "1";
            var simulation = this.GetSimulationById(DEFAULT_SIMULATION_ID);

            this.simulationsService
                .Setup(x => x.InsertAsync(It.IsAny<Simulation>(), "default"))
                .ReturnsAsync(simulation);

            // Act
            var result = this.target.PostAsync(
                (SimulationApiModel) null,
                "default"
            ).Result;

            // Assert
            Assert.Equal(DEFAULT_SIMULATION_ID, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItInvokesDeleteOnceWhenDeleteSimulationWithId()
        {
            // Arrange
            const string ID = "1";

            // Act
            this.target.DeleteAsync(ID)
                .Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.simulationsService.Verify(x => x.DeleteAsync(ID), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItInvokesMetricsServiceOnceWhenQueryIotHubMetrics()
        {
            // Arrange
            const string ID = "1";

            // Act
            this.target.PostAsync(
                    new MetricsRequestsApiModel(),
                    ID)
                .Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.iothubMetrics
                .Verify(x => x.GetIothubMetricsAsync(
                    It.IsAny<MetricsRequestListModel>()
                ), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsNullWhenGetSimulationByIdNotFound()
        {
            // Arrange
            const string ID = "1";
            var simulation = this.GetSimulationById(ID);

            // Act
            var result = this.target.GetAsync(ID).Result;

            // Assert
            Assert.Null(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheListOfSimulations()
        {
            // Arrange
            var simulations = this.GetSimulations();

            this.simulationsService
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(simulations);

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal(simulations.Count, result.Items.Count);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheSimulationById()
        {
            // Arrange
            const string ID = "1";
            var simulation = this.GetSimulationById(ID);

            this.simulationsService
                .Setup(x => x.GetAsync(ID))
                .ReturnsAsync(simulation);

            // Act
            var result = this.target.GetAsync(ID).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(simulation.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenCreateASimulationWithInValidInput()
        {
            // Arrange
            var simulation = new Simulation();

            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await this.target.PostAsync(
                        SimulationApiModel.FromServiceModel(simulation)
                    )
                )
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItUpdatesSimulationThroughPatchtMethod()
        {
            // Arrange
            const string DEFAULT_SIMULATION_ID = "1";
            var simulation = this.GetSimulationById(DEFAULT_SIMULATION_ID);

            this.simulationsService
                .Setup(x => x.MergeAsync(It.IsAny<SimulationPatch>()))
                .ReturnsAsync(simulation);

            // Act
            var result = this.target.PatchAsync(
                DEFAULT_SIMULATION_ID,
                new SimulationPatchApiModel
                {
                    ETag = simulation.ETag
                }
            ).Result;

            // Assert
            Assert.Equal(DEFAULT_SIMULATION_ID, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItUpdatesSimulationThroughPutMethod()
        {
            // Arrange
            const string DEFAULT_SIMULATION_ID = "1";
            var simulation = this.GetSimulationById(DEFAULT_SIMULATION_ID);

            this.simulationsService
                .Setup(x => x.UpsertAsync(It.IsAny<Simulation>()))
                .ReturnsAsync(simulation);

            // Act
            var simulationApiModel =
                SimulationApiModel.FromServiceModel(
                    simulation);

            var result = this.target.PutAsync(
                simulationApiModel,
                DEFAULT_SIMULATION_ID
            ).Result;

            // Assert
            Assert.Equal(DEFAULT_SIMULATION_ID, result.Id);
        }

        private Simulation GetSimulationById(string id)
        {
            return new Simulation
            {
                Id = id,
                DeviceModels = new List<Simulation.DeviceModelRef>
                {
                    new Simulation.DeviceModelRef { Id = "Chiller_01", Count = 10 }
                },
                StartTime = DateTimeOffset.UtcNow,
                EndTime = DateTimeOffset.UtcNow.AddHours(2),
                IotHubConnectionStrings = new List<string>
                {
                    ""
                }
            };
        }

        private List<Simulation> GetSimulations()
        {
            return new List<Simulation>
            {
                new Simulation { ETag = "ETag_1" },
                new Simulation { ETag = "ETag_2" },
                new Simulation { ETag = "ETag_3" }
            };
        }
    }
}
