// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel;
using System.Collections.Generic;
using WebService.Test.helpers;
using Xunit;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

namespace WebService.Test.v1.Models
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
            Assert.IsType<DeviceModelMessageSchema>(result);
        }


        private DeviceModelMessageSchema GetDeviceModelMessageSchema()
        {
            var schema = new DeviceModelMessageSchema()
            {
                Name = "chiller-sensors",
                Format = DeviceModelMessageSchemaFormat.JSON,
                Fields = new Dictionary<string, DeviceModelMessageSchemaType>()
                {
                    { "cargotemperature", DeviceModelMessageSchemaType.Double }
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
