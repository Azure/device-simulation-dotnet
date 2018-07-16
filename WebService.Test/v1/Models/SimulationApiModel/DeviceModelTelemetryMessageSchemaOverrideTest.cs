// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.SimulationApiModel
{
    public class DeviceModelTelemetryMessageSchemaOverrideTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelApiModelTelemetryMessageSchemaOverrideFromServiceModel()
        {
            // Arrange
            var messageSchemaOverride = this.GetMessageSchemaOverride();

            // Act
            var result = DeviceModelTelemetryMessageSchemaOverride.FromServiceModel(messageSchemaOverride);

            // Assert
            Assert.IsType<DeviceModelTelemetryMessageSchemaOverride>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelTelemetryMessageSchemaOverrideFromApiModel()
        {
            // Arrange
            var messageSchemaOverride = this.GetDeviceModelApiModelTelemetryMessageSchemaOverride();

            // Act
            var result = messageSchemaOverride.ToServiceModel();

            // Assert
            Assert.IsType<Simulation.DeviceModelTelemetryMessageSchemaOverride>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsNullWhenDeviceModelTelemetryMessageSchemaOverrideIsEmpty()
        {
            // Arrange
            var messageSchemaOverride = this.GetEmptyDeviceModelApiModelTelemetryMessageSchemaOverride();

            // Act
            var result = messageSchemaOverride.ToServiceModel();

            // Assert
            Assert.Null(result);
        }

        private DeviceModelTelemetryMessageSchemaOverride GetDeviceModelApiModelTelemetryMessageSchemaOverride()
        {
            var messageSchemaOverride =  new DeviceModelTelemetryMessageSchemaOverride()
            {
                Name = "chiller",
                Format = "JSON",
                Fields = new Dictionary<string, string>()
                {
                    { "cargotemperature", "Double" }
                }
            };

            return messageSchemaOverride;
        }

        private DeviceModelTelemetryMessageSchemaOverride GetEmptyDeviceModelApiModelTelemetryMessageSchemaOverride()
        {
            var messageSchemaOverride = new DeviceModelTelemetryMessageSchemaOverride()
            {
                Name = "",
                Format = "",
                Fields = new Dictionary<string, string>()
            };

            return messageSchemaOverride;
        }

        private Simulation.DeviceModelTelemetryMessageSchemaOverride GetMessageSchemaOverride()
        {
            var messageSchemaOverride = new Simulation.DeviceModelTelemetryMessageSchemaOverride();

            return messageSchemaOverride;
        }
    }
}
