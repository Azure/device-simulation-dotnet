// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.SimulationApiModel
{
    public class DeviceModelTelemetryOverrideTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelTelemetryOverrideFromServiceModel()
        {
            // Arrange
            var telemetryOverride = this.GetDeviceModelTelemetryOverride();

            // Act
            var result = DeviceModelTelemetryOverride.FromServiceModel(telemetryOverride);

            // Assert
            Assert.IsType<DeviceModelTelemetryOverride>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelOverrideFromDeviceModelApiModel()
        {
            // Arrange
            var telemetryOverride = this.GetDeviceModelApiModelTelemetryOverride();

            // Act
            var result = telemetryOverride.ToServiceModel();

            // Assert
            Assert.IsType<Simulation.DeviceModelTelemetryOverride>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsNullWhenDeviceModelApiModelTelemetryOverrideIsEmpty()
        {
            // Arrange
            var telemetryOverride = this.GetEmptyDeviceModelApiModelTelemetryOverride();

            // Act
            var result = telemetryOverride.ToServiceModel();

            // Assert
            Assert.Null(result);
        }

        private DeviceModelTelemetryOverride GetDeviceModelApiModelTelemetryOverride()
        {
            var telemetryOverride = new DeviceModelTelemetryOverride()
            {
                Interval = "00:10:00",
                MessageTemplate = "template",
                MessageSchema = new DeviceModelTelemetryMessageSchemaOverride()
            };

            return telemetryOverride;
        }

        private DeviceModelTelemetryOverride GetEmptyDeviceModelApiModelTelemetryOverride()
        {
            var telemetryOverride = new DeviceModelTelemetryOverride();

            return telemetryOverride;
        }

        private Simulation.DeviceModelTelemetryOverride GetDeviceModelTelemetryOverride()
        {
            var telemetryOverride = new Simulation.DeviceModelTelemetryOverride();

            return telemetryOverride;
        }
    }
}
