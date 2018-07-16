// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel;
using Moq;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.DeviceModelApiModel
{
    public class DeviceModelSimulationScriptTest
    {
        private readonly Mock<ILogger> logger;

        public DeviceModelSimulationScriptTest()
        {
            this.logger = new Mock<ILogger>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelSimulationScriptFromServiceModel()
        {
            // Arrange
            var script = this.GetScript();

            // Act
            var result = DeviceModelSimulationScript.FromServiceModel(script);

            // Assert
            Assert.IsType<DeviceModelSimulationScript>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsScriptFromDeviceModelSimulationScript()
        {
            // Arrange
            var script = this.GetDeviceModelSimulationScript();

            // Act
            var result = script.ToServiceModel();

            // Assert
            Assert.IsType<Script>(result);
        }

        private DeviceModelSimulationScript GetDeviceModelSimulationScript()
        {
            var script = new DeviceModelSimulationScript
            {
                Type = ScriptInterpreter.JAVASCRIPT_SCRIPT,
                Path = "scripts"
            };

            return script;
        }

        private Script GetScript()
        {
            var script = new Script
            {
                Type = ScriptInterpreter.JAVASCRIPT_SCRIPT,
                Path = "scripts"
            };

            return script;
        }
    }
}
