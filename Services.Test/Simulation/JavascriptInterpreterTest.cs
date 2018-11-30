// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Moq;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace Services.Test.Simulation
{
    public class JavascriptInterpreterTest
    {
        // Used to capture application logs
        private readonly ITestOutputHelper log;

        private readonly Mock<IServicesConfig> config;
        private readonly Mock<ILogger> logger;
        private readonly Mock<ISmartDictionary> properties;
        private readonly Mock<IDeviceModelScripts> simulationScripts;
        private readonly JavascriptInterpreter target;

        public JavascriptInterpreterTest(ITestOutputHelper log)
        {
            this.log = log;

            this.config = new Mock<IServicesConfig>();
            this.config.SetupGet(x => x.DeviceModelsFolder).Returns("./data/devicemodels/");
            this.config.SetupGet(x => x.DeviceModelsScriptsFolder).Returns("./data/devicemodels/scripts/");
            this.properties = new Mock<ISmartDictionary>();
            this.simulationScripts = new Mock<IDeviceModelScripts>();
            this.logger = new Mock<ILogger>();

            this.target = new JavascriptInterpreter(
                this.simulationScripts.Object,
                this.config.Object,
                this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReturnedStateIsIntact()
        {
            // Arrange
            SmartDictionary deviceState = new SmartDictionary();
            
            var script = new Script
            {
                Path = "chiller-01-state.js"
            };
            var context = new Dictionary<string, object>
            {
                ["currentTime"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                ["deviceId"] = "device-123",
                ["deviceModel"] = "room"
            };
            var state = new Dictionary<string, object>
            {
                ["temperature"] = 50.5,
                ["temperature_unit"] = "device-123",
                ["humidity"] = 70.2,
                ["humidity_unit"] = "%",
                ["lights_on"] = false,
                ["pressure"] = 150.0,
                ["pressure_unit"] = "psig"
            };

            deviceState.SetAll(state);

            // Act
            this.target.Invoke(script, context, deviceState, this.properties.Object);

            // Assert
            Assert.Equal(state.Count, deviceState.GetAll().Count);
            Assert.IsType<Double>(deviceState.Get("temperature"));
            Assert.IsType<string>(deviceState.Get("temperature_unit"));
            Assert.IsType<Double>(deviceState.Get("humidity"));
            Assert.IsType<string>(deviceState.Get("humidity_unit"));
            Assert.IsType<bool>(deviceState.Get("lights_on"));
            Assert.IsType<Double>(deviceState.Get("pressure"));
            Assert.IsType<string>(deviceState.Get("pressure_unit"));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void TestJavascriptFiles()
        {
            // Arrange
            SmartDictionary deviceState = new SmartDictionary();

            var files = new List<Script>
            {
                new Script { Path = "chiller-01-state.js" },
                new Script { Path = "chiller-02-state.js" },
                new Script { Path = "elevator-01-state.js" },
                new Script { Path = "elevator-02-state.js" },
                new Script { Path = "engine-01-state.js" },
                new Script { Path = "engine-02-state.js" },
                new Script { Path = "prototype-01-state.js" },
                new Script { Path = "prototype-02-state.js" },
                new Script { Path = "truck-01-state.js" },
                new Script { Path = "truck-02-state.js" }
            };
            var context = new Dictionary<string, object>
            {
                ["currentTime"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                ["deviceId"] = "device-123",
                ["deviceModel"] = "room"
            };

            // Act - Assert (no exception should occur)
            foreach (var file in files)
            {
                this.target.Invoke(file, context, deviceState, this.properties.Object);
                Assert.NotNull(deviceState.GetAll());
            }
        }
    }
}
