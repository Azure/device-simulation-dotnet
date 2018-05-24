// Copyright (c) Microsoft. All rights reserved.

using Moq;
using System.Collections.Generic;
using DeviceModel = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;
using Xunit.Abstractions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;
using Xunit;
using Services.Test.helpers;
using System;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

namespace Services.Test
{
    public class DeviceModelsGenerationTest
    {
        private readonly Mock<ILogger> logger;
        private readonly DeviceModelsGeneration target;

        public DeviceModelsGenerationTest()
        {
            this.logger = new Mock<ILogger>();
            this.target = new DeviceModelsGeneration(this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void OverrideWithoutMessageSchema()
        {
            // Arrange
            var source = new DeviceModel
            {
                Telemetry = new List<DeviceModelMessage>
                {
                    new DeviceModelMessage
                    {
                        Interval = TimeSpan.Zero,
                        MessageTemplate = string.Empty,
                        MessageSchema = new DeviceModelMessageSchema
                        {
                            Name = "sensor-01",
                            Format = DeviceModelMessageSchemaFormat.JSON,
                            Fields = new Dictionary<string, DeviceModelMessageSchemaType>
                            {
                                { "temp", new DeviceModelMessageSchemaType() }
                            }
                        }
                    }
                }
            };   

            var overrideInfo = new DeviceModelOverride();

            // Act
            var result = this.target.Generate(source, overrideInfo);

            // Assert
            Assert.NotEmpty(result.Telemetry);
            for (var i = 0; i < result.Telemetry.Count; i++)
            {
                Assert.Equal(source.Telemetry[i].MessageTemplate, result.Telemetry[i].MessageTemplate);
                Assert.Equal(source.Telemetry[i].MessageSchema.Name, result.Telemetry[i].MessageSchema.Name);
            }
        }
    }
}
