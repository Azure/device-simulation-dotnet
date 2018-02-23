// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Moq;
using SimulationAgent.Test.helpers;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace SimulationAgent.Test.DeviceState
{
    public class DeviceStateActorTest
    {
        private readonly Mock<ILogger> logger;
        private readonly Mock<IScriptInterpreter> scriptInterpreter;
        private readonly Mock<UpdateDeviceState> updateDeviceStateLogic;
        private readonly DeviceStateActor target;

        public DeviceStateActorTest(ITestOutputHelper log)
        {
            this.logger = new Mock<ILogger>();
            this.scriptInterpreter = new Mock<IScriptInterpreter>();
            this.updateDeviceStateLogic = new Mock<UpdateDeviceState>(
                this.scriptInterpreter.Object,
                this.logger.Object);

            this.target = new DeviceStateActor(
                this.logger.Object,
                this.updateDeviceStateLogic.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReportInactiveStatusBeforeRun()
        {
            // Arrange
            SetupDeviceStatusActor();

            // Act
            var result = this.target.IsDeviceActive;

            // Assert
            Assert.False(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReportActiveStatusAfterRun()
        {
            // Arrange
            SetupDeviceStatusActor();

            // Act
            this.target.Run();
            var result = this.target.IsDeviceActive;

            // Assert
            Assert.True(result);
        }

        private void SetupDeviceStatusActor()
        {
            string DEVICE_ID = "01";
            int postion = 1;
            int total = 10;
            var deviceModel = new DeviceModel { Id = DEVICE_ID };
            var deviceState = new Dictionary<string, object>
            {
                { DEVICE_ID, new Object { } }
            };

            this.scriptInterpreter
                .Setup(x => x.Invoke(
                    It.IsAny<Script>(),
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<Dictionary<string, object>>()))
                .Returns(deviceState);

            this.target.Setup(DEVICE_ID, deviceModel, postion, total);
        }
    }
}
