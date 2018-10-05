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
    public class DisconnectTest
    {
        private const string DEVICE_ID = "01";

        private readonly Mock<ILogger> logger;
        private readonly Mock<IDevices> devices;
        private readonly Mock<IScriptInterpreter> scriptInterpreter;
        private readonly Mock<IDeviceClient> deviceClient;
        private readonly Mock<IDeviceStateActor> deviceStateActor;
        private readonly Mock<IDeviceConnectionActor> deviceConnectionActor;
        private readonly Mock<IRateLimitingConfig> rateLimitingConfig;
        private readonly Mock<ConnectionLoopSettings> loopSettings;
        private readonly Mock<IInstance> mockInstance;

        private readonly DeviceModel deviceModel;
        private readonly Disconnect target;

        public DisconnectTest(ITestOutputHelper log)
        {
            this.logger = new Mock<ILogger>();
            this.devices = new Mock<IDevices>();
            this.scriptInterpreter = new Mock<IScriptInterpreter>();
            this.rateLimitingConfig = new Mock<IRateLimitingConfig>();
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.deviceConnectionActor = new Mock<IDeviceConnectionActor>();
            this.deviceClient = new Mock<IDeviceClient>();
            this.loopSettings = new Mock<ConnectionLoopSettings>(this.rateLimitingConfig.Object);
            this.mockInstance = new Mock<IInstance>();
            this.deviceModel = new DeviceModel { Id = DEVICE_ID };

            this.target = new Disconnect(
                this.scriptInterpreter.Object, 
                this.logger.Object,
                this.mockInstance.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task ItSetsStateToDeviceDisconnected()
        {
            // Arrange
            this.SetupDeviceConnectionActor();
            this.target.Init(this.deviceConnectionActor.Object, DEVICE_ID, this.deviceModel);

            // Act
            await this.target.RunAsync();

            // Assert
            this.deviceClient.Verify(x => x.DisconnectAsync());
            this.deviceConnectionActor.Verify(x => x.HandleEvent(DeviceConnectionActor.ActorEvents.Disconnected));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task ItSetsStateToDeviceDisconnectionFailed()
        {
            // Arrange
            this.SetupDeviceConnectionActor();
            this.target.Init(this.deviceConnectionActor.Object, DEVICE_ID, this.deviceModel);
            this.deviceConnectionActor.Setup(x => x.Client).Throws<Exception>();

            // Act
            await this.target.RunAsync();

            // Assert
            this.deviceConnectionActor.Verify(x => x.HandleEvent(DeviceConnectionActor.ActorEvents.DisconnectionFailed));
        }

        private void SetupDeviceConnectionActor()
        {
            // Setup the SimulationContext
            var testSimulation = new Simulation();
            var mockSimulationContext = new Mock<ISimulationContext>();
            mockSimulationContext.Object.InitAsync(testSimulation).Wait(Constants.TEST_TIMEOUT);
            mockSimulationContext.SetupGet(x => x.Devices).Returns(this.devices.Object);

            this.deviceConnectionActor.SetupGet(x => x.SimulationContext).Returns(mockSimulationContext.Object);
            this.deviceConnectionActor.Setup(x => x.Client).Returns(this.deviceClient.Object);
        }
    }
}
