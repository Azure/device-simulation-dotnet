// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Moq;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.DeviceModelApiModel
{
    public class DeviceModelScriptTest
    {
        private readonly Mock<ILogger> logger;

        public DeviceModelScriptTest()
        {
            this.logger = new Mock<ILogger>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelSimulationScriptFromServiceModel()
        {
            // Arrange
            var script = this.GetScript();

            // Act
            var result = Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelScript.FromServiceModel(script);

            // Assert
            Assert.IsType<Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelScript>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsScriptFromDeviceModelScript()
        {
            // Arrange
            var script = this.GetDeviceModelScript();

            // Act
            var result = script.ToServiceModel();

            // Assert
            Assert.IsType<Script>(result);
        }

        private Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelScript GetDeviceModelScript()
        {
            var script = new Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel.DeviceModelScript
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
