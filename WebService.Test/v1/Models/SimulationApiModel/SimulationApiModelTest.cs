// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
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

        public SimulationApiModelTest()
        {
            this.logger = new Mock<ILogger>();
            this.connectionStringManager = new Mock<IIotHubConnectionStringManager>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsSimulationApiModelFromServiceModel()
        {
            // Arrange
            var simulation = this.GetSimulationModel();

            // Act
            var result = Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel.SimulationApiModel.FromServiceModel(simulation);

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
            var result = simulationApiModel.ToServiceModel(simulationApiModel.Id);

            // Assert
            Assert.IsType<Simulation>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void NoExceptionThrownForValidDeviceModelApiModel()
        {
            // Arrange
            var simulationApiModel = this.GetSimulationApiModel();
            this.SetupConnectionStringManager();

            // Act
            var task = Record.ExceptionAsync(async () => await simulationApiModel.ValidateInputRequest(this.logger.Object, this.connectionStringManager.Object));

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
                    async () => await simulationApiModel.ValidateInputRequest(this.logger.Object, this.connectionStringManager.Object))
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
                    async () => await simulationApiModel.ValidateInputRequest(this.logger.Object, this.connectionStringManager.Object))
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
                    async () => await simulationApiModel.ValidateInputRequest(this.logger.Object, this.connectionStringManager.Object))
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
                IotHub = new SimulationIotHub("HostName=[hubname];SharedAccessKeyName=[iothubowner];SharedAccessKey=[valid key]")
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
