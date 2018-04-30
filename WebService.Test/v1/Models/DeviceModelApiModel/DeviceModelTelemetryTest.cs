// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel;
using Moq;
using System;
using System.Collections.Generic;
using WebService.Test.helpers;
using Xunit;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

namespace WebService.Test.v1.Models
{
    public class DeviceModelTelemetryTest
    {
        private readonly Mock<ILogger> logger;

        public DeviceModelTelemetryTest()
        {
            this.logger = new Mock<ILogger>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelTelemetryFromServiceModel()
        {
            // Arrange
            var message = this.GetDeviceModelMessage();

            // Act
            var result = DeviceModelTelemetry.FromServiceModel(message);

            // Assert
            Assert.IsType<DeviceModelTelemetry>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelMessageFromDeviceModelTelemetry()
        {
            // Arrange
            var telemetry = this.GetDeviceModelTelemetry();

            // Act
            var result = DeviceModelTelemetry.ToServiceModel(telemetry);

            // Assert
            Assert.IsType<DeviceModelMessage>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void NoExceptionThrownForValidDeviceModelTelemetry()
        {
            // Arrange
            var telemetry = this.GetDeviceModelTelemetry();

            // Act
            var ex = Record.Exception(() => telemetry.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.Null(ex);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidInterval()
        {
            // Arrange
            DeviceModelTelemetry InvalidInterval(DeviceModelTelemetry model)
            {
                model.Interval = "";
                return model;
            }

            var telemetry = this.GetInvalidDeviceModelTelemetry(InvalidInterval);

            // Act
            var ex = Record.Exception(() => telemetry.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.IsType<BadRequestException>(ex);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidMessageTemplate()
        {
            // Arrange
            DeviceModelTelemetry InvalidMessageTemplate(DeviceModelTelemetry model)
            {
                model.MessageTemplate = "";
                return model;
            }

            var telemetry = this.GetInvalidDeviceModelTelemetry(InvalidMessageTemplate);

            // Act
            var ex = Record.Exception(() => telemetry.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.IsType<BadRequestException>(ex);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidMessageSchema()
        {
            // Arrange
            DeviceModelTelemetry InvalidMessageSchema(DeviceModelTelemetry model)
            {
                model.MessageSchema = new DeviceModelTelemetryMessageSchema();
                return model;
            }

            var telemetry = this.GetInvalidDeviceModelTelemetry(InvalidMessageSchema);

            // Act
            var ex = Record.Exception(() => telemetry.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.IsType<BadRequestException>(ex);
        }

        private DeviceModelTelemetry GetInvalidDeviceModelTelemetry(Func<DeviceModelTelemetry, DeviceModelTelemetry> func)
        {
            var model = this.GetDeviceModelTelemetry();
            return func(model);
        }

        private DeviceModelTelemetry GetDeviceModelTelemetry()
        {
            var telemetry = new DeviceModelTelemetry()
            {
                Interval = "00:10:00",
                MessageTemplate = "{\"cargotemperature\":${cargotemperature},\"cargotemperature_unit\":\"${cargotemperature_unit}\"}",
                MessageSchema = new DeviceModelTelemetryMessageSchema()
                {
                    Name = "truck",
                    Format = "JSON",
                    Fields = new Dictionary<string, string>()
                    {
                        { "cargotemperature", "double" }
                    }
                }
            };

            return telemetry;
        }

        private DeviceModelMessage GetDeviceModelMessage()
        {
            var telemetry = new DeviceModelMessage()
            {
                Interval = TimeSpan.Parse("00:10:00"),
                MessageTemplate = "{\"cargotemperature\":${cargotemperature},\"cargotemperature_unit\":\"${cargotemperature_unit}\"}",
                MessageSchema = new DeviceModelMessageSchema()
                {
                    Name = "truck",
                    Format = DeviceModelMessageSchemaFormat.JSON,
                    Fields = new Dictionary<string, DeviceModelMessageSchemaType>()
                    {
                        { "cargotemperature", DeviceModelMessageSchemaType.Double }
                    }
                }
            };

            return telemetry;
        }
    }
}
