// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.SimulationApiModel
{
    public class DeviceModelSimulationScriptOverrideTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelSimulationScriptApiModelOverrideFromServiceModel()
        {
            // Arrange
            var scriptOverride = this.GetScriptOverride();

            // Act
            var result = DeviceModelSimulationScriptOverride.FromServiceModel(scriptOverride);

            // Assert
            Assert.IsType<DeviceModelSimulationScriptOverride>(result.FirstOrDefault());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelSimulationScriptOverrideFromDeviceModelSimulationScriptApiModel()
        {
            // Arrange
            var scriptApiModelOverride = this.GetDeviceModelSimulationScriptApiModelOverride();

            // Act
            var result = scriptApiModelOverride.ToServiceModel();

            // Assert
            Assert.IsType<Simulation.DeviceModelSimulationScriptOverride>(result);
        }

        private DeviceModelSimulationScriptOverride GetDeviceModelSimulationScriptApiModelOverride()
        {
            var scriptOverride = new DeviceModelSimulationScriptOverride()
            {
                Type = "",
                Path = "",
                Params = ""
            };

            return scriptOverride;
        }

        private IList<Simulation.DeviceModelSimulationScriptOverride> GetScriptOverride()
        {
            var scriptOverride =  new List<Simulation.DeviceModelSimulationScriptOverride>()
            {
                new Simulation.DeviceModelSimulationScriptOverride()
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
