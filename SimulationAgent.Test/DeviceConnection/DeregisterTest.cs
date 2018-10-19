// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Moq;
using SimulationAgent.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace SimulationAgent.Test.DeviceConnection
{
    public class DeregisterTest
    {
        private const string DEVICE_ID = "01";

        private readonly Mock<ILogger> logger;
        private Mock<IDevices> devices;
        private readonly Mock<IScriptInterpreter> scriptInterpreter;
        private readonly Mock<IDeviceStateActor> deviceStateActor;
        private readonly Mock<IDeviceConnectionActor> mockDeviceContext;
        private readonly Mock<IRateLimitingConfig> rateLimitingConfig;
        private readonly Mock<ConnectionLoopSettings> loopSettings;
        private readonly Mock<IInstance> mockInstance;

        private DeviceModel deviceModel;
        private Deregister target;

        public DeregisterTest(ITestOutputHelper log)
        {
            this.logger = new Mock<ILogger>();
            this.devices = new Mock<IDevices>();
            this.scriptInterpreter = new Mock<IScriptInterpreter>();
            this.rateLimitingConfig = new Mock<IRateLimitingConfig>();
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.mockDeviceContext = new Mock<IDeviceConnectionActor>();
            this.loopSettings = new Mock<ConnectionLoopSettings>(this.rateLimitingConfig.Object);
            this.mockInstance = new Mock<IInstance>();
            this.deviceModel = new DeviceModel { Id = DEVICE_ID };

            this.target = new Deregister(this.logger.Object, this.mockInstance.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task ItSetsStateToDeviceDeregistered()
        {
            // Arrange
            this.SetupDeviceConnectionActor();
            this.target.Init(this.mockDeviceContext.Object, DEVICE_ID, this.deviceModel);

            // Act
            await this.target.RunAsync();

            // Assert
            this.devices.Verify(m => m.DeleteAsync(DEVICE_ID));
            this.mockDeviceContext.Verify(x => x.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceDeregistered));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task ItSetsStateToDeviceDeregisterationFailed()
        {
            // Arrange
            this.SetupDeviceConnectionActor();
            this.target.Init(this.mockDeviceContext.Object, DEVICE_ID, this.deviceModel);
            this.devices.Setup(x => x.DeleteAsync(It.IsAny<string>())).Throws<Exception>();

            // Act
            await this.target.RunAsync();

            // Assert
            this.mockDeviceContext.Verify(x => x.HandleEvent(DeviceConnectionActor.ActorEvents.DeregisterationFailed));
        }

        private void SetupDeviceConnectionActor()
        {
            var testSimulation = new Simulation();
            var mockSimulationContext = new Mock<ISimulationContext>();
            mockSimulationContext.SetupGet(x => x.Devices).Returns(this.devices.Object);
            mockSimulationContext.Object.InitAsync(testSimulation).Wait(Constants.TEST_TIMEOUT);

            this.mockDeviceContext.SetupGet(x => x.SimulationContext).Returns(mockSimulationContext.Object);
        }
    }
}
