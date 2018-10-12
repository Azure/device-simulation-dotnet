// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel;
using Moq;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.SimulationApiModel
{
    public class SimulationApiModelTest
    {
        private readonly Mock<ILogger> logger;
        private readonly Mock<IIotHubConnectionStringManager> connectionStringManager;
        private readonly Mock<IServicesConfig> servicesConfig;
        private readonly Mock<IDeploymentConfig> deploymentConfig;
        private readonly Mock<ISimulationRunner> simulationRunner;
        private readonly Mock<IRateLimiting> rateReporter;

        public SimulationApiModelTest()
        {
            this.logger = new Mock<ILogger>();
            this.connectionStringManager = new Mock<IIotHubConnectionStringManager>();
            this.servicesConfig = new Mock<IServicesConfig>();
            this.deploymentConfig = new Mock<IDeploymentConfig>();
            this.simulationRunner = new Mock<ISimulationRunner>();
            this.rateReporter = new Mock<IRateLimiting>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsSimulationApiModelFromServiceModel()
        {
            // Arrange
            var simulation = this.GetSimulationModel();

            // Act
            var result = Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel.FromServiceModelAsync(
                    simulation, this.servicesConfig.Object, this.deploymentConfig.Object, this.connectionStringManager.Object, this.simulationRunner.Object, this.rateReporter.Object)
                .CompleteOrTimeout().Result;

            // Assert
            Assert.IsType<Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel>(result);
            Assert.Equal(simulation.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsSimulationModelFromSimulationApiModel()
        {
            // Arrange
            var simulationApiModel = this.GetSimulationApiModel();

            // Act
            var result = simulationApiModel.ToServiceModel(null, simulationApiModel.Id);

            // Assert
            Assert.IsType<Simulation>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItKeepsInternalReadonlyProperties()
        {
            // Arrange
            var simulationApiModel = this.GetSimulationApiModel();

            var existingSimulation1 = new Simulation
            {
                PartitioningComplete = true,
                StoppedTime = DateTimeOffset.UtcNow.AddHours(-10)
            };

            var existingSimulation2 = new Simulation
            {
                PartitioningComplete = false,
                StoppedTime = DateTimeOffset.UtcNow.AddHours(-20)
            };

            // Act
            var result1 = simulationApiModel.ToServiceModel(existingSimulation1, simulationApiModel.Id);
            var result2 = simulationApiModel.ToServiceModel(existingSimulation2, simulationApiModel.Id);

            // Assert
            Assert.True(result1.PartitioningComplete);
            Assert.False(result2.PartitioningComplete);
            Assert.Equal(existingSimulation1.StoppedTime, result1.StoppedTime);
            Assert.Equal(existingSimulation2.StoppedTime, result2.StoppedTime);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void NoExceptionThrownForValidDeviceModelApiModel()
        {
            // Arrange
            var simulationApiModel = this.GetSimulationApiModel();
            this.SetupConnectionStringManager();

            // Act
            var task = Record.ExceptionAsync(async () => await simulationApiModel.ValidateInputRequestAsync(this.logger.Object, this.connectionStringManager.Object));

            // Assert
            Assert.Null(task.Exception);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidDeviceModels()
        {
            // Arrange
            Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel InvalidDeviceModels(Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel model)
            {
                model.DeviceModels = new List<SimulationDeviceModelRef>();
                return model;
            }

            var simulationApiModel = this.GetInvalidSimulationApiModel(InvalidDeviceModels);
            this.SetupConnectionStringManager();

            // Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await simulationApiModel.ValidateInputRequestAsync(this.logger.Object, this.connectionStringManager.Object))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidDeviceModelsCount()
        {
            // Arrange
            Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel InvalidDeviceModels(Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel model)
            {
                model.DeviceModels = new List<SimulationDeviceModelRef>() { new SimulationDeviceModelRef() { Count = 0 } };
                return model;
            }

            var simulationApiModel = this.GetInvalidSimulationApiModel(InvalidDeviceModels);
            this.SetupConnectionStringManager();

            // Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await simulationApiModel.ValidateInputRequestAsync(this.logger.Object, this.connectionStringManager.Object))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidTimes()
        {
            // Arrange
            Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel InvalidDates(Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel model)
            {
                model.StartTime = "2017-05-31T01:21:37+00:00";
                model.EndTime = "2017-05-31T01:21:37+00:00";
                return model;
            }

            var simulationApiModel = this.GetInvalidSimulationApiModel(InvalidDates);
            this.SetupConnectionStringManager();

            // Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await simulationApiModel.ValidateInputRequestAsync(this.logger.Object, this.connectionStringManager.Object))
                .Wait(Constants.TEST_TIMEOUT);
        }

        private void SetupConnectionStringManager()
        {
            this.connectionStringManager
                .Setup(x => x.ValidateConnectionStringAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        private Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel GetInvalidSimulationApiModel(Func<Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel, Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel> func)
        {
            var model = this.GetSimulationApiModel();
            return func(model);
        }

        private Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel GetSimulationApiModel()
        {
            var simulationApiModel = new Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel
            {
                Id = "id",
                ETag = "etag",
                StartTime = DateTimeOffset.UtcNow.ToString(),
                EndTime = DateTimeOffset.UtcNow.AddHours(1).ToString(),
                Enabled = false,
                DeviceModels = new List<SimulationDeviceModelRef>()
                {
                    new SimulationDeviceModelRef()
                    {
                        Id = "device_id",
                        Count = 1
                    }
                },
                IotHubs = new List<SimulationIotHub> { new SimulationIotHub("HostName=[hubname];SharedAccessKeyName=[iothubowner];SharedAccessKey=[valid key]") }
            };

            return simulationApiModel;
        }

        private Simulation GetSimulationModel()
        {
            var simulation = new Simulation()
            {
                Id = "id",
                ETag = "etag",
                DeviceModels = new List<Simulation.DeviceModelRef>()
                {
                    new Simulation.DeviceModelRef { Id = "01", Count = 1 }
                }
            };

            return simulation;
        }
    }
}
