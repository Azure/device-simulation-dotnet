// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
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
        private readonly Mock<IDeviceConnectionActor> deviceConnectionActor;
        private readonly Mock<IRateLimitingConfig> rateLimitingConfig;
        private readonly Mock<ConnectionLoopSettings> loopSettings;

        private DeviceModel deviceModel;
        private Deregister target;

        public DeregisterTest(ITestOutputHelper log)
        {
            this.logger = new Mock<ILogger>();
            this.devices = new Mock<IDevices>();
            this.scriptInterpreter = new Mock<IScriptInterpreter>();
            this.rateLimitingConfig = new Mock<IRateLimitingConfig>();
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.deviceConnectionActor = new Mock<IDeviceConnectionActor>();
            this.loopSettings = new Mock<ConnectionLoopSettings>(this.rateLimitingConfig.Object);
            this.deviceModel = new DeviceModel { Id = DEVICE_ID };

            this.target = new Deregister(this.devices.Object, this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task ItSetsStateToDeviceDeregistered()
        {
            // Arrange
            this.SetupDeviceConnectionActor();
            this.target.SetupAsync(this.deviceConnectionActor.Object, DEVICE_ID, this.deviceModel)
                .Wait(Constants.TEST_TIMEOUT);
            
            // Act
            await this.target.RunAsync();

            // Assert
            this.devices.Verify(m => m.DeleteAsync(DEVICE_ID));
            this.deviceConnectionActor.Verify(x => x.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceDeregistered));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task ItSetsStateToDeviceDeregisterationFailed()
        {
            // Arrange
            this.SetupDeviceConnectionActor();
            this.target.SetupAsync(this.deviceConnectionActor.Object, DEVICE_ID, this.deviceModel)
                .Wait(Constants.TEST_TIMEOUT);
            this.devices.Setup(x => x.DeleteAsync(It.IsAny<string>())).Throws<Exception>();

            // Act
            await this.target.RunAsync();

            // Assert
            this.deviceConnectionActor.Verify(x => x.HandleEvent(DeviceConnectionActor.ActorEvents.DeregisterationFailed));
        }

        private void SetupDeviceConnectionActor()
        {
            this.deviceConnectionActor.Object.SetupAsync(
                    DEVICE_ID,
                    this.deviceModel,
                    this.deviceStateActor.Object,
                    this.loopSettings.Object)
                .Wait(Constants.TEST_TIMEOUT);
        }
    }
}
