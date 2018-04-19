// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Controllers;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Models.DeviceModelApiModel;
using Moq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v2.Controllers
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
        public async Task GetReturnsTheListOfDeviceModels()
        {
            // Arrange
            var deviceModels = this.GetDeviceModels();
            
            this.deviceModelsService
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(deviceModels);

            // Act
            var result = await this.target.GetAsync();

            // Assert
            Assert.Equal(deviceModels.Count, result.Items.Count);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task GetReturnsTheDeviceModelById()
        {
            // Arrange
            const string id = "deviceModelId";
            var deviceModel = this.GetDeviceModelById(id);

            this.deviceModelsService
                .Setup(x => x.GetAsync(id))
                .ReturnsAsync(deviceModel);

            // Act
            var result = await this.target.GetAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModel.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task GetReturnsNullWithInvalidId()
        {
            // Arrange
            const string id = "deviceModelId";

            // Act
            var result = await this.target.GetAsync(id);

            // Assert
            Assert.Null(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task PostCreatesTheDeviceModelWithValidInput()
        {
            // Arrange
            const string id = "deviceModelId";
            var deviceModel = this.GetDeviceModelById(id);
            DeviceModelApiModel deviceModelAPIModel = GetValidDeviceModelApiModel(id);

            this.deviceModelsService
                .Setup(x => x.InsertAsync(It.IsAny<DeviceModel>()))
                .ReturnsAsync(deviceModel);

            // Act
            var result = await this.target.PostAsync(deviceModelAPIModel);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModel.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task PostThrowsErrorWithInvalidInput()
        {
            // Arrange
            const string id = "deviceModelId";
            var deviceModel = this.GetDeviceModelById(id);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => this.target.PostAsync(DeviceModelApiModel.FromServiceModel(deviceModel)));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task PutCreatesTheDeviceModelWithValidInput()
        {
            // Arrange
            const string id = "deviceModelId";
            var deviceModel = this.GetDeviceModelById(id);
            DeviceModelApiModel deviceModelAPIModel = GetValidDeviceModelApiModel(id);

            this.deviceModelsService
                .Setup(x => x.UpsertAsync(It.IsAny<DeviceModel>()))
                .ReturnsAsync(deviceModel);

            // Act
            var result = await this.target.PutAsync(deviceModelAPIModel);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModel.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task PutThrowsErrorWithInvalidInput()
        {
            // Arrange
            const string id = "deviceModelId";
            var deviceModel = this.GetDeviceModelById(id);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => this.target.PutAsync(DeviceModelApiModel.FromServiceModel(deviceModel)));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task DeleteInvokesMethodWithId()
        {
            // Arrange
            const string id = "deviceModelId";
            var deviceModel = this.GetDeviceModelById(id);

            // Act
            await this.target.DeleteAsync(id);

            // Assert
            this.deviceModelsService.Verify(x => x.DeleteAsync(id));
        }

        private static DeviceModelApiModel GetValidDeviceModelApiModel(string id)
        {
            return new DeviceModelApiModel()
            {
                Id = id,
                Protocol = "AMQP",
                Telemetry = new List<DeviceModelTelemetry>()
                {
                    new DeviceModelTelemetry()
                    {
                        Interval = "00:00:10",
                        MessageTemplate = "template",
                        MessageSchema = new DeviceModelTelemetryMessageSchema()
                        {
                            Name = "name",
                            Format = "JSON",
                            Fields = new Dictionary<string, string>()
                            {
                                { "key", "value" }
                            }
                        }
                    }
                },
                Simulation = new DeviceModelSimulation()
                {
                    Interval = "00:00:10",
                    Scripts = new List<DeviceModelSimulationScript>()
                    {
                        new DeviceModelSimulationScript()
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
                ETag = "Etag_1"
            };

            return deviceModel;
        }
    }
}
