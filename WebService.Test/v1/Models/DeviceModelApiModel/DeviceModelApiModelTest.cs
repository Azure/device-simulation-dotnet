// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
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
            var ex = Record.Exception(() => deviceModelApiModel.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.Null(ex);
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

            var deviceModelApiModel = this.GetInvalidDeviceModelApiModel(InvalidProtocol);
            var deviceModelApiModelWithEmptyProtocol = this.GetInvalidDeviceModelApiModel(EmptyProtocol);

            // Act
            var ex = Record.Exception(() => deviceModelApiModel.ValidateInputRequest(this.logger.Object));
            var exception = Record.Exception(() => deviceModelApiModelWithEmptyProtocol.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.IsType<BadRequestException>(ex);
            Assert.IsType<BadRequestException>(exception);
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

            // Act
            var ex = Record.Exception(() => deviceModelApiModel.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.IsType<BadRequestException>(ex);
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

            // Act
            var ex = Record.Exception(() => deviceModelApiModel.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.IsType<BadRequestException>(ex);
        }

        private Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel GetValidDeviceModelApiModel()
        {
            var deviceModelApiModel = new Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelApiModel
            {
                Id = "id",
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
