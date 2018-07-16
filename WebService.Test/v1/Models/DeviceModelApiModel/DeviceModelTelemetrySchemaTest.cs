// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.DeviceModelApiModel
{
    public class DeviceModelTelemetrySchemaTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelTelemetrySchemaFromServiceModel()
        {
            // Arrange
            var schema = this.GetDeviceModelMessageSchema();

            // Act
            var result = DeviceModelTelemetryMessageSchema.FromServiceModel(schema);

            // Assert
            Assert.IsType<DeviceModelTelemetryMessageSchema>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelMessageSchemaFromDeviceModelTelemetryMessageSchema()
        {
            // Arrange
            var schema = this.GetDeviceModelTelemetryMessageSchema();

            // Act
            var result = DeviceModelTelemetryMessageSchema.ToServiceModel(schema);

            // Assert
            Assert.IsType<DeviceModel.DeviceModelMessageSchema>(result);
        }


        private DeviceModel.DeviceModelMessageSchema GetDeviceModelMessageSchema()
        {
            var schema = new DeviceModel.DeviceModelMessageSchema()
            {
                Name = "chiller-sensors",
                Format = DeviceModel.DeviceModelMessageSchemaFormat.JSON,
                Fields = new Dictionary<string, DeviceModel.DeviceModelMessageSchemaType>()
                {
                    { "cargotemperature", DeviceModel.DeviceModelMessageSchemaType.Double }
                }
            };

            return schema;
        }

        private DeviceModelTelemetryMessageSchema GetDeviceModelTelemetryMessageSchema()
        {
            var schema = new DeviceModelTelemetryMessageSchema()
            {
                Name = "chiller-sensors",
                Format = "JSON",
                Fields = new Dictionary<string, string>()
                {
                    { "cargotemperature", "double" }
                }
            };

            return schema;
        }
    }
}
