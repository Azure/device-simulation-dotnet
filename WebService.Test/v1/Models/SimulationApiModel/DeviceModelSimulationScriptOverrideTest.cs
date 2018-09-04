// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.SimulationApiModel
{
    public class DeviceModelScriptOverrideTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelScriptApiModelOverrideFromServiceModel()
        {
            // Arrange
            var scriptOverride = this.GetScriptOverride();

            // Act
            var result = DeviceModeScriptOverride.FromServiceModel(scriptOverride);

            // Assert
            Assert.IsType<DeviceModeScriptOverride>(result.FirstOrDefault());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelSimulationScriptOverrideFromDeviceModelSimulationScriptApiModel()
        {
            // Arrange
            var scriptApiModelOverride = this.GetDeviceModelScriptApiModelOverride();

            // Act
            var result = scriptApiModelOverride.ToServiceModel();

            // Assert
            Assert.IsType<Simulation.DeviceModelScriptOverride>(result);
        }

        private DeviceModeScriptOverride GetDeviceModelScriptApiModelOverride()
        {
            var scriptOverride = new DeviceModeScriptOverride()
            {
                Type = "",
                Path = "",
                Params = ""
            };

            return scriptOverride;
        }

        private IList<Simulation.DeviceModelScriptOverride> GetScriptOverride()
        {
            var scriptOverride =  new List<Simulation.DeviceModelScriptOverride>()
            {
                new Simulation.DeviceModelScriptOverride()
                {
                    Type = "",
                    Path = "",
                    Params = ""
                }
            };

            return scriptOverride;
        }
    }
}
