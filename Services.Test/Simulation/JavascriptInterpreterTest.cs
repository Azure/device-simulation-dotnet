// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
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
        private readonly JavascriptInterpreter target;

        public JavascriptInterpreterTest(ITestOutputHelper log)
        {
            this.log = log;

            this.config = new Mock<IServicesConfig>();
            this.config.SetupGet(x => x.DeviceModelsFolder).Returns("./data/devicemodels/");
            this.config.SetupGet(x => x.DeviceModelsScriptsFolder).Returns("./data/devicemodels/scripts/");

            this.logger = new Mock<ILogger>();
            this.CaptureApplicationLogs(this.logger);

            this.target = new JavascriptInterpreter(this.config.Object, this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReturnedStateIsIntact()
        {
            // Arrange
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
                ["lights_on"] = false
            };

            // Act
            Dictionary<string, object> result = this.target.Invoke(filename, context, state);

            // Assert
            Assert.Equal(state.Count, result.Count);
            Assert.IsType<Double>(result["temperature"]);
            Assert.IsType<string>(result["temperature_unit"]);
            Assert.IsType<Double>(result["humidity"]);
            Assert.IsType<string>(result["humidity_unit"]);
            Assert.IsType<bool>(result["lights_on"]);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void TestJavascriptFiles()
        {
            // Arrange
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
                var result = this.target.Invoke(file, context, null);
                Assert.NotNull(result);
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
