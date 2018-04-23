﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Models.SimulationApiModel;
using System.Collections.Generic;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v2.Models
{
    public class DeviceModelApiModelOverrideTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelApiModelOverrideFromServiceModel()
        {
            // Arrange
            var deviceModelOverride = this.GetDeviceModelOverride();

            // Act
            var result = DeviceModelApiModelOverride.FromServiceModel(deviceModelOverride);

            // Assert
            Assert.IsType<DeviceModelApiModelOverride>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelOverrideFromDeviceModelApiModel()
        {
            // Arrange
            var deviceModelApiModelOverride = this.GetDeviceModelApiModelOverride();

            // Act
            var result = deviceModelApiModelOverride.ToServiceModel();

            // Assert
            Assert.IsType<Simulation.DeviceModelOverride>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsNullWhenDeviceModelApiModelOverrideIsEmpty()
        {
            // Arrange
            var deviceModelApiModelOverride = this.GetDeviceModelApiModelOverride();

            // Act
            var result = deviceModelApiModelOverride.ToServiceModel();

            // Assert
            Assert.Null(result);
        }

        private DeviceModelApiModelOverride GetDeviceModelApiModelOverride()
        {
            var deviceModelApiModelOverride = new DeviceModelApiModelOverride()
            {
                Simulation = new DeviceModelSimulationOverride()
                {
                    Interval = "00:10:00"
                },
                Telemetry = new List<DeviceModelTelemetryOverride>()
                {
                    new DeviceModelTelemetryOverride()
                }
            };

            return deviceModelApiModelOverride;
        }

        private DeviceModelApiModelOverride GetEmptyDeviceModelApiModelOverride()
        {
            var deviceModelApiModelOverride = new DeviceModelApiModelOverride()
            {
                Simulation = new DeviceModelSimulationOverride(),
                Telemetry = new List<DeviceModelTelemetryOverride>()
            };

            return deviceModelApiModelOverride;
        }

        private Simulation.DeviceModelOverride GetDeviceModelOverride()
        {
            var deviceModelOverride = new Simulation.DeviceModelOverride()
            {
                Simulation = new Simulation.DeviceModelSimulationOverride(),
                Telemetry = new List<Simulation.DeviceModelTelemetryOverride>()
            };

            return deviceModelOverride;
        }
    }
}