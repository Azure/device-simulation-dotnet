// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Moq;
using SimulationAgent.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace SimulationAgent.Test.DeviceConnection
{
    public class ConnectTest
    {
        private readonly Connect target;

        private readonly Mock<ILogger> log;
        private readonly Mock<IInstance> instance;
        private Mock<IDeviceConnectionActor> deviceContext;
        private Mock<ISimulationContext> simulationContext;

        public ConnectTest(ITestOutputHelper log)
        {
            this.log = new Mock<ILogger>();
            this.instance = new Mock<IInstance>();
            this.deviceContext = new Mock<IDeviceConnectionActor>();

            this.target = new Connect(
                this.log.Object,
                this.instance.Object);

            this.ActorInit();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDisposeExistingClientBeforeCreatingANewOne()
        {
            // Arrange
            var callSequence = "";
            var devices = new Mock<IDevices>();
            devices.Setup(x => x.GetClient(It.IsAny<Device>(), It.IsAny<IoTHubProtocol>()))
                .Callback(() => callSequence += "create;");
            this.simulationContext.SetupGet(x => x.Devices).Returns(devices.Object);
            this.deviceContext.Setup(x => x.DisposeClient())
                .Callback(() => callSequence += "dispose;");

            // Act
            this.target.RunAsync().CompleteOrTimeout();

            // Assert
            Assert.Equal("dispose;create;", callSequence);
            this.deviceContext.Verify(x => x.DisposeClient(), Times.Once);
            devices.Verify(x => x.GetClient(It.IsAny<Device>(), It.IsAny<IoTHubProtocol>()), Times.Once);
        }

        private void ActorInit()
        {
            var deviceId = "abc";
            var deviceModel = new DeviceModel();

            this.simulationContext = new Mock<ISimulationContext>();
            this.deviceContext = new Mock<IDeviceConnectionActor>();
            this.deviceContext.SetupGet(x => x.SimulationContext).Returns(this.simulationContext.Object);

            this.target.Init(this.deviceContext.Object, deviceId, deviceModel);
        }
    }
}
