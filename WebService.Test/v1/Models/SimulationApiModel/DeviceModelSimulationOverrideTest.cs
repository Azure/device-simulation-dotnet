// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.SimulationApiModel
{
    public class DeviceModelSimulationOverrideTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelSimulationlOverrideFromServiceModel()
        {
            // Arrange
            var deviceModelSimulationOverride = this.GetDeviceModelSimulationOverride();

            // Act
            var result = DeviceModelSimulationOverride.FromServiceModel(deviceModelSimulationOverride);

            // Assert
            Assert.IsType<DeviceModelSimulationOverride>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelSimulationlOverrideFromApiModel()
        {
            // Arrange
            var deviceModelSimulationOverrideApiModel = this.GetDeviceModelSimulationOverrideApiModel();

            // Act
            var result = deviceModelSimulationOverrideApiModel.ToServiceModel();

            // Assert
            Assert.IsType<Simulation.DeviceModelSimulationOverride>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsNullWhenDeviceModelSimulationOverrideApiModelIsEmpty()
        {
            // Arrange
            var deviceModelSimulationOverrideApiModel = this.GetEmptyDeviceModelSimulationOverrideAPIModel();

            // Act
            var result = deviceModelSimulationOverrideApiModel.ToServiceModel();

            // Assert
            Assert.Null(result);
        }

        private DeviceModelSimulationOverride GetEmptyDeviceModelSimulationOverrideAPIModel()
        {
            var deviceModelSimulationOverrideApiModel = new DeviceModelSimulationOverride();

            return deviceModelSimulationOverrideApiModel;
        }

        private DeviceModelSimulationOverride GetDeviceModelSimulationOverrideApiModel()
        {
            var deviceModelSimulationOverrideApiModel = new DeviceModelSimulationOverride()
            {
                Interval = "00:10:00",
                Scripts = new List<DeviceModelSimulationScriptOverride>()
                {
                    new DeviceModelSimulationScriptOverride()
                }
            };

            return deviceModelSimulationOverrideApiModel;
        }

        private Simulation.DeviceModelSimulationOverride GetDeviceModelSimulationOverride()
        {
            var deviceModelSimulationOverride = new Simulation.DeviceModelSimulationOverride();

            return deviceModelSimulationOverride;
        }
    }
}
