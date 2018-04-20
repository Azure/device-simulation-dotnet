// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Models.DeviceModelApiModel;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v2.Models
{
    public class DeviceModelApiModelTest
    {
        private readonly Mock<ILogger> logger;

        public DeviceModelApiModelTest()
        {
            this.logger = new Mock<ILogger>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelFromServiceModel()
        {
            // Arrange
            var deviceModel = this.GetDeviceModel();

            // Act
            var result = DeviceModelApiModel.FromServiceModel(deviceModel);

            // Assert
            Assert.IsType<DeviceModelApiModel>(result);
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
            var ex = Record.Exception(() => deviceModelApiModel.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.Null(ex);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidProtocol()
        {
            // Arrange
            Func<DeviceModelApiModel, DeviceModelApiModel> invalidProtocol = delegate (DeviceModelApiModel model)
            {
                model.Protocol = "";
                return model;
            };
            var deviceModelApiModel = GetInvalidDeviceModelApiModel(invalidProtocol);

            // Act
            var ex = Record.Exception(() => deviceModelApiModel.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.IsType<BadRequestException>(ex);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidId()
        {
            // Arrange
            Func<DeviceModelApiModel, DeviceModelApiModel> invalidId = delegate (DeviceModelApiModel model)
            {
                model.Id = "";
                return model;
            };
            var deviceModelApiModel = GetInvalidDeviceModelApiModel(invalidId);

            // Act
            var ex = Record.Exception(() => deviceModelApiModel.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.IsType<BadRequestException>(ex);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidTelemetry()
        {
            // Arrange
            Func<DeviceModelApiModel, DeviceModelApiModel> invalidTelemetry = delegate (DeviceModelApiModel model)
            {
                model.Telemetry = new List<DeviceModelTelemetry>();
                return model;
            };
            var deviceModelApiModel = GetInvalidDeviceModelApiModel(invalidTelemetry);

            // Act
            var ex = Record.Exception(() => deviceModelApiModel.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.IsType<BadRequestException>(ex);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidSimulation()
        {
            // Arrange
            Func<DeviceModelApiModel, DeviceModelApiModel> invalidSimulation = delegate (DeviceModelApiModel model)
            {
                model.Simulation = new DeviceModelSimulation();
                return model;
            };
            var deviceModelApiModel = GetInvalidDeviceModelApiModel(invalidSimulation);

            // Act
            var ex = Record.Exception(() => deviceModelApiModel.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.IsType<BadRequestException>(ex);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForCustomModelWithInvalidEtag()
        {
            // Arrange
            Func<DeviceModelApiModel, DeviceModelApiModel> invalidEtag = delegate (DeviceModelApiModel model)
            {
                model.Type = "CustomModel";
                model.ETag = "";
                return model;
            };
            var deviceModelApiModel = GetInvalidDeviceModelApiModel(invalidEtag);

            // Act
            var ex = Record.Exception(() => deviceModelApiModel.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.IsType<BadRequestException>(ex);
        }

        private DeviceModelApiModel GetValidDeviceModelApiModel()
        {
            var deviceModelApiModel = new DeviceModelApiModel
            {
                Id = "id",
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

            return deviceModelApiModel;
        }

        private DeviceModelApiModel GetInvalidDeviceModelApiModel(Func<DeviceModelApiModel, DeviceModelApiModel> func)
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
