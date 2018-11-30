// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Moq;
using SimulationAgent.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace SimulationAgent.Test.DeviceProperties
{
    public class SetDeviceTagTest
    {
        private const string DEVICE_ID = "01";

        private readonly Mock<ILogger> logger;
        private readonly Mock<IInstance> instance;
        private readonly Mock<IDevices> devices;
        private readonly Mock<IDevicePropertiesActor> devicePropertiesActor;
        private readonly Mock<IDeviceStateActor> deviceStateActor;
        private readonly Mock<IDeviceConnectionActor> mockDeviceContext;
        private readonly Mock<IRateLimitingConfig> rateLimitingConfig;
        private readonly Mock<PropertiesLoopSettings> loopSettings;
        private readonly SetDeviceTag target;

        public SetDeviceTagTest(ITestOutputHelper log)
        {
            this.logger = new Mock<ILogger>();
            this.instance = new Mock<IInstance>();
            this.devices = new Mock<IDevices>();
            this.rateLimitingConfig = new Mock<IRateLimitingConfig>();
            this.devicePropertiesActor = new Mock<IDevicePropertiesActor>();
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.mockDeviceContext = new Mock<IDeviceConnectionActor>();
            this.loopSettings = new Mock<PropertiesLoopSettings>(this.rateLimitingConfig.Object);

            this.target = new SetDeviceTag(this.logger.Object, this.instance.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Should_Call_DeviceTagged_When_Succeeded()
        {
            // Arrange
            this.SetupPropertiesActor();
            this.target.Init(this.devicePropertiesActor.Object, DEVICE_ID, this.devices.Object);

            // Act
            this.target.RunAsync().Wait();

            // Assert
            this.devicePropertiesActor.Verify(x => x.HandleEvent(DevicePropertiesActor.ActorEvents.DeviceTagged));
        }

        private void SetupPropertiesActor()
        {
            // Setup a SimulationContext object
            var testSimulation = new Simulation();
            var mockSimulationContext = new Mock<ISimulationContext>();
            mockSimulationContext.Object.InitAsync(testSimulation).Wait(Constants.TEST_TIMEOUT);
            mockSimulationContext.SetupGet(x => x.Devices).Returns(this.devices.Object);

            this.devicePropertiesActor.Object.Init(
                mockSimulationContext.Object,
                DEVICE_ID,
                this.deviceStateActor.Object,
                this.mockDeviceContext.Object,
                this.loopSettings.Object);
        }
    }
}
