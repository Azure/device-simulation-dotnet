// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
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
        private readonly Mock<IDevices> devices;
        private readonly Mock<IDevicePropertiesActor> devicePropertiesActor;
        private readonly Mock<IDeviceStateActor> deviceStateActor;
        private readonly Mock<IDeviceConnectionActor> deviceConnectionActor;
        private readonly Mock<IRateLimitingConfig> rateLimitingConfig;
        private readonly Mock<PropertiesLoopSettings> loopSettings;
        private readonly SetDeviceTag target;

        public SetDeviceTagTest(ITestOutputHelper log)
        {
            this.logger = new Mock<ILogger>();
            this.devices = new Mock<IDevices>();
            this.rateLimitingConfig = new Mock<IRateLimitingConfig>();
            this.devicePropertiesActor = new Mock<IDevicePropertiesActor>();
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.deviceConnectionActor = new Mock<IDeviceConnectionActor>();
            this.loopSettings = new Mock<PropertiesLoopSettings>(this.rateLimitingConfig.Object);

            this.target = new SetDeviceTag(this.devices.Object, this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Should_Call_DeviceTagged_When_Succeeded()
        {
            // Arrange
            this.SetupPropertiesActor();
            this.target.Setup(this.devicePropertiesActor.Object, DEVICE_ID);

            // Act
            this.target.RunAsync().Wait();

            // Assert
            this.devicePropertiesActor.Verify(x => x.HandleEvent(DevicePropertiesActor.ActorEvents.DeviceTagged));
        }

        private void SetupPropertiesActor()
        {
            this.devicePropertiesActor.Object.Setup(
                DEVICE_ID,
                this.deviceStateActor.Object,
                this.deviceConnectionActor.Object,
                this.loopSettings.Object);
        }
    }
}
