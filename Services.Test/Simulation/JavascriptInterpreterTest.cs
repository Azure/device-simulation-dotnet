// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Moq;
using Newtonsoft.Json;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace Services.Test.Simulation
{
    public class JavascriptInterpreterTest
    {
        // Set to `true` to debug a failing test, e.g. capture more logs
        private readonly bool debug = false;

        private readonly ITestOutputHelper log;
        private readonly Mock<IServicesConfig> config;
        private readonly Mock<ILogger> logger;
        private readonly Mock<ISmartDictionary> properties;
        private readonly JavascriptInterpreter target;

        public JavascriptInterpreterTest(ITestOutputHelper log)
        {
            this.log = log;

            this.config = new Mock<IServicesConfig>();
            this.config.SetupGet(x => x.DeviceModelsFolder).Returns("./data/devicemodels/");
            this.config.SetupGet(x => x.DeviceModelsScriptsFolder).Returns("./data/devicemodels/scripts/");
            this.properties = new Mock<ISmartDictionary>();

            this.logger = new Mock<ILogger>();
            this.CaptureApplicationLogs(this.logger);

            this.target = new JavascriptInterpreter(this.config.Object, this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReturnedStateIsIntact()
        {
            // Arrange
            SmartDictionary deviceState = new SmartDictionary();

            var filename = "chiller-01-state.js";
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
            this.target.Invoke(filename, context, deviceState, this.properties.Object);

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

            var files = new List<string>
            {
                "chiller-01-state.js",
                "chiller-02-state.js",
                "elevator-01-state.js",
                "elevator-02-state.js",
                "engine-01-state.js",
                "engine-02-state.js",
                "prototype-01-state.js",
                "prototype-02-state.js",
                "truck-01-state.js",
                "truck-02-state.js"
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

        private void CaptureApplicationLogs(Mock<ILogger> l)
        {
            if (!this.debug) return;

            l.Setup(x => x.Debug(It.IsAny<string>(), It.IsAny<Action>()))
                .Callback((string s, Action a) => { this.log.WriteLine(s); });

            l.Setup(x => x.Info(It.IsAny<string>(), It.IsAny<Action>()))
                .Callback((string s, Action a) => { this.log.WriteLine(s); });

            l.Setup(x => x.Warn(It.IsAny<string>(), It.IsAny<Action>()))
                .Callback((string s, Action a) => { this.log.WriteLine(s); });

            l.Setup(x => x.Error(It.IsAny<string>(), It.IsAny<Action>()))
                .Callback((string s, Action a) => { this.log.WriteLine(s); });

            l.Setup(x => x.Debug(It.IsAny<string>(), It.IsAny<Func<object>>()))
                .Callback((string s, Func<object> f) =>
                {
                    this.log.WriteLine(s);
                    this.log.WriteLine(JsonConvert.SerializeObject(f.Invoke(), Formatting.Indented));
                });
        }
    }
}
