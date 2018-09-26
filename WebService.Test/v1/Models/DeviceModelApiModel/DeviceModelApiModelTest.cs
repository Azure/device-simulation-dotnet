// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel;
using Moq;
using Newtonsoft.Json.Linq;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.DeviceModelApiModel
{
    public class DeviceModelApiModelTest
    {
        private readonly Mock<ILogger> logger;
        private readonly Mock<IDeviceModels> deviceModels;

        public DeviceModelApiModelTest()
        {
            this.logger = new Mock<ILogger>();
            this.deviceModels = new Mock<IDeviceModels>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelFromServiceModel()
        {
            // Arrange
            var deviceModel = this.GetDeviceModel();

            // Act
            var result = Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel.FromServiceModel(deviceModel);

            // Assert
            Assert.IsType<Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel>(result);
            Assert.Equal(deviceModel.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelApiModelFromDeviceModel()
        {
            // Arrange
            var deviceModelApiModel = this.GetValidDeviceModelApiModel();

            // Act
            var result = deviceModelApiModel.ToServiceModel(deviceModelApiModel.Id);

            // Assert
            Assert.IsType<DeviceModel>(result);
            Assert.Equal(deviceModelApiModel.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void NoExceptionThrownForValidDeviceModelApiModel()
        {
            // Arrange
            var deviceModelApiModel = this.GetValidDeviceModelApiModel();

            // Act
            var task = Record.ExceptionAsync(async () => await deviceModelApiModel.ValidateInputRequest(this.logger.Object, this.deviceModels.Object));

            // Assert
            Assert.Null(task.Exception);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidProtocol()
        {
            // Arrange
            Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel EmptyProtocol(Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel model)
            {
                model.Protocol = "";
                return model;
            }

            Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel InvalidProtocol(Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel model)
            {
                model.Protocol = "AMTT";
                return model;
            }

            var deviceModelApiModelWithInvalidProtocol = this.GetInvalidDeviceModelApiModel(InvalidProtocol);
            var deviceModelApiModelWithEmptyProtocol = this.GetInvalidDeviceModelApiModel(EmptyProtocol);

            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await deviceModelApiModelWithInvalidProtocol.ValidateInputRequest(this.logger.Object, this.deviceModels.Object))
                .Wait(Constants.TEST_TIMEOUT);
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await deviceModelApiModelWithEmptyProtocol.ValidateInputRequest(this.logger.Object, this.deviceModels.Object))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidTelemetry()
        {
            // Arrange
            Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel InvalidTelemetry(Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel model)
            {
                model.Telemetry = new List<DeviceModelTelemetry>();
                return model;
            }

            var deviceModelApiModel = this.GetInvalidDeviceModelApiModel(InvalidTelemetry);

            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await deviceModelApiModel.ValidateInputRequest(this.logger.Object, this.deviceModels.Object))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidSimulation()
        {
            // Arrange
            Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel InvaildSimulation(Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel model)
            {
                model.Simulation = new DeviceModelSimulation();
                return model;
            }

            var deviceModelApiModel = this.GetInvalidDeviceModelApiModel(InvaildSimulation);

            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await deviceModelApiModel.ValidateInputRequest(this.logger.Object, this.deviceModels.Object))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForEmptyName()
        {
            // Arrange
            Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel NoName(Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel model)
            {
                model.Name = "";
                return model;
            }

            var deviceModelApiModel = this.GetInvalidDeviceModelApiModel(NoName);

            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await deviceModelApiModel.ValidateInputRequest(this.logger.Object, this.deviceModels.Object))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForDuplicateNameWhenInsert()
        {
            // Arrange
            const string EXISTING_NAME = "name";

            Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel DupName(Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel model)
            {
                model.Name = EXISTING_NAME;
                return model;
            }

            var deviceModelApiModel = this.GetInvalidDeviceModelApiModel(DupName);
            this.deviceModels.Setup(x => x.GetListAsync())
                .ReturnsAsync(new List<DeviceModel>
                {
                    new DeviceModel { Name = EXISTING_NAME }
                });

            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await deviceModelApiModel.ValidateInputRequest(this.logger.Object, this.deviceModels.Object))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForDuplicateNameWhenUpsert()
        {
            // Arrange
            const string NEW_NAME = "new name";
            const string EXISTING_NAME = "name";

            Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel DupName(Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel model)
            {
                model.Name = NEW_NAME;
                return model;
            }

            var deviceModelApiModel = this.GetInvalidDeviceModelApiModel(DupName);
            this.deviceModels.Setup(x => x.GetListAsync())
                .ReturnsAsync(new List<DeviceModel>
                {
                    new DeviceModel { Name = EXISTING_NAME },
                    new DeviceModel { Name = NEW_NAME }
                });

            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await deviceModelApiModel.ValidateInputRequest(this.logger.Object, this.deviceModels.Object, true))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItAllowDuplicateNameForEditExistDeivceModel()
        {
            // Arrange
            const string EXISTING_NAME = "name";

            Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel DupName(Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel model)
            {
                model.Name = EXISTING_NAME;
                return model;
            }

            var deviceModelApiModel = this.GetInvalidDeviceModelApiModel(DupName);
            this.deviceModels.Setup(x => x.GetListAsync())
                .ReturnsAsync(new List<DeviceModel>
                {
                    new DeviceModel { Name = EXISTING_NAME }
                });

            // Act
            var task = Record.ExceptionAsync(async () => await deviceModelApiModel.ValidateInputRequest(this.logger.Object, this.deviceModels.Object));

            // Assert
            Assert.Null(task.Exception);
        }

        private Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel GetValidDeviceModelApiModel()
        {
            var deviceModelApiModel = new Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel
            {
                Id = "id",
                Name = "name",
                Type = "Custom",
                ETag = "Etag_1",
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
                    Scripts = new List<Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelScript>()
                    {
                        new Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelScript()
                        {
                            Type = "type",
                            Path = "path",
                            Params = JObject.Parse("{\"ccc\":{\"Min\":\"1\",\"Max\":\"11\",\"Step\":1,\"Unit\":\"y\"}}")
                        }
                    }
                }
            };

            return deviceModelApiModel;
        }

        private Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel GetInvalidDeviceModelApiModel(Func<Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel, Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel> func)
        {
            var model = this.GetValidDeviceModelApiModel();
            return func(model);
        }

        private DeviceModel GetDeviceModel()
        {
            var deviceModel = new DeviceModel
            {
                Id = "id",
                ETag = "Etag_1"
            };

            return deviceModel;
        }
    }
}
