// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Moq;
using SimulationAgent.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace SimulationAgent.Test.DeviceState
{
    public class DeviceStateActorTest
    {
        private readonly Mock<ILogger> mockLogger;
        private readonly Mock<IScriptInterpreter> mockScriptInterpreter;
        private readonly Mock<UpdateDeviceState> mockUpdateDeviceStateLogic;
        private readonly Mock<IInstance> mockInstance;
        private readonly DeviceStateActor target;

        public DeviceStateActorTest(ITestOutputHelper log)
        {
            this.mockLogger = new Mock<ILogger>();
            this.mockScriptInterpreter = new Mock<IScriptInterpreter>();
            this.mockInstance = new Mock<IInstance>();
            this.mockUpdateDeviceStateLogic = new Mock<UpdateDeviceState>(
                this.mockLogger.Object,
                this.mockInstance.Object);

            this.target = new DeviceStateActor(
                this.mockUpdateDeviceStateLogic.Object,
                this.mockScriptInterpreter.Object,
                this.mockLogger.Object,
                this.mockInstance.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReportInactiveStatusBeforeRun()
        {
            // Arrange
            this.SetupDeviceStateActor();

            // Act
            var result = this.target.IsDeviceActive;

            // Assert
            Assert.False(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReportActiveStatusAfterRun()
        {
            // Arrange
            this.SetupDeviceStateActor();

            // Act
            this.target.Run();
            var result = this.target.IsDeviceActive;

            // Assert
            Assert.True(result);
        }

        private void SetupDeviceStateActor()
        {
            string DEVICE_ID = "01";
            int position = 1;
            var deviceModel = new DeviceModel { Id = DEVICE_ID };

            var mockSimulationContext = new Mock<ISimulationContext>();

            this.target.Init(
                mockSimulationContext.Object,
                DEVICE_ID,
                deviceModel,
                position);
        }
    }
}
