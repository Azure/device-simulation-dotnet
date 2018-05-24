// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel;
using Moq;
using Newtonsoft.Json.Linq;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Controllers
{
    public class DeviceModelsControllerTest
    {
        private readonly Mock<IDeviceModels> deviceModelsService;
        private readonly Mock<ILogger> logger;
        private readonly DeviceModelsController target;

        public DeviceModelsControllerTest()
        {
            this.deviceModelsService = new Mock<IDeviceModels>();
            this.logger = new Mock<ILogger>();

            this.target = new DeviceModelsController(
                this.deviceModelsService.Object,
                this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetReturnsTheListOfDeviceModels()
        {
            // Arrange
            var deviceModels = this.GetDeviceModels();

            this.deviceModelsService
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(deviceModels);

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal(deviceModels.Count, result.Items.Count);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetReturnsTheDeviceModelById()
        {
            // Arrange
            const string ID = "deviceModelId";
            var deviceModel = this.GetDeviceModelById(ID);

            this.deviceModelsService
                .Setup(x => x.GetAsync(ID))
                .ReturnsAsync(deviceModel);

            // Act
            var result = this.target.GetAsync(ID).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModel.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetReturnsNullWithInvalidId()
        {
            // Arrange
            const string ID = "deviceModelId";

            // Act
            var result = this.target.GetAsync(ID).Result;

            // Assert
            Assert.Null(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void PostCreatesTheDeviceModelWithValidInput()
        {
            // Arrange
            const string ID = "deviceModelId";
            var deviceModel = this.GetDeviceModelById(ID);
            DeviceModelApiModel deviceModelApiModel = GetValidDeviceModelApiModel(ID);

            this.deviceModelsService
                .Setup(x => x.InsertAsync(It.IsAny<DeviceModel>()))
                .ReturnsAsync(deviceModel);

            // Act
            var result = this.target.PostAsync(deviceModelApiModel).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModel.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void PostThrowsErrorWithInvalidInput()
        {
            // Arrange
            const string ID = "deviceModelId";
            var deviceModel = this.GetDeviceModelById(ID);

            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await this.target.PostAsync(DeviceModelApiModel.FromServiceModel(deviceModel)))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void PutCreatesTheDeviceModelWithValidInput()
        {
            // Arrange
            const string ID = "deviceModelId";
            var deviceModel = this.GetDeviceModelById(ID);
            DeviceModelApiModel deviceModelApiModel = GetValidDeviceModelApiModel(ID);

            this.deviceModelsService
                .Setup(x => x.UpsertAsync(It.IsAny<DeviceModel>()))
                .ReturnsAsync(deviceModel);

            // Act
            var result = this.target.PutAsync(deviceModelApiModel).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModel.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void PutThrowsErrorWithInvalidInput()
        {
            // Arrange
            const string ID = "deviceModelId";
            var deviceModel = this.GetDeviceModelById(ID);

            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(
                async () => await this.target.PutAsync(DeviceModelApiModel.FromServiceModel(deviceModel)))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void DeleteInvokesDeviceModelServiceWithId()
        {
            // Arrange
            const string ID = "deviceModelId";

            // Act
            this.target.DeleteAsync(ID)
                .Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.deviceModelsService.Verify(x => x.DeleteAsync(ID), Times.Once);
        }

        private static DeviceModelApiModel GetValidDeviceModelApiModel(string id)
        {
            return new DeviceModelApiModel
            {
                Id = id,
                Protocol = "AMQP",
                ETag = "Etag",
                Type = "Custom",
                Telemetry = new List<DeviceModelTelemetry>
                {
                    new DeviceModelTelemetry
                    {
                        Interval = "00:00:10",
                        MessageTemplate = "template",
                        MessageSchema = new DeviceModelTelemetryMessageSchema
                        {
                            Name = "name",
                            Format = "JSON",
                            Fields = new Dictionary<string, string>
                            {
                                { "key", "value" }
                            }
                        }
                    }
                },
                Simulation = new DeviceModelSimulation
                {
                    Interval = "00:00:10",
                    Scripts = new List<DeviceModelSimulationScript>
                    {
                        new DeviceModelSimulationScript
                        {
                            Type = "type",
                            Path = "path",
                            Params = JObject.Parse("{\"ccc\":{\"Min\":\"1\",\"Max\":\"11\",\"Step\":1,\"Unit\":\"y\"}}")
                        }
                    }
                }
            };
        }

        private List<DeviceModel> GetDeviceModels()
        {
            var deviceModels = new List<DeviceModel>
            {
                new DeviceModel { Id = "Id_1", ETag = "Etag_1" },
                new DeviceModel { Id = "Id_2", ETag = "Etag_2" },
                new DeviceModel { Id = "Id_3", ETag = "Etag_3" },
                new DeviceModel { Id = "Id_4", ETag = "Etag_4" },
                new DeviceModel { Id = "Id_5", ETag = "Etag_5" }
            };

            return deviceModels;
        }

        private DeviceModel GetDeviceModelById(string id)
        {
            var deviceModel = new DeviceModel
            {
                Id = id,
                ETag = "Etag_1",
                Type = DeviceModel.DeviceModelType.Custom
            };

            return deviceModel;
        }
    }
}
