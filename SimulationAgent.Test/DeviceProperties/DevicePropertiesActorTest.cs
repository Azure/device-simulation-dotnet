// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Moq;
using SimulationAgent.Test.helpers;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties.DevicePropertiesActor;

namespace SimulationAgent.Test.DeviceProperties
{
    public class DevicePropertiesActorTest
    {
        private readonly Mock<ILogger> logger;
        private readonly Mock<IActorsLogger> actorsLogger;
        private readonly Mock<IRateLimiting> rateLimiting;
        private readonly Mock<IRateLimitingConfig> rateLimitingConfig;
        private readonly Mock<IDevices> devices;
        private readonly Mock<UpdateReportedProperties> updatePropertiesLogic;
        private readonly Mock<SetDeviceTag> deviceTagLogic;
        private readonly Mock<IDeviceConnectionActor> deviceConnectionActor;
        private readonly Mock<IDeviceStateActor> deviceStateActor;
        private readonly Mock<PropertiesLoopSettings> loopSettings;

        private const string DEVICE_ID = "01";
        private const int TWIN_WRITES_PER_SECOND = 10;

        private DevicePropertiesActor target;

        public DevicePropertiesActorTest(ITestOutputHelper log)
        {
            this.logger = new Mock<ILogger>();
            this.actorsLogger = new Mock<IActorsLogger>();
            this.rateLimiting = new Mock<IRateLimiting>();
            this.rateLimitingConfig = new Mock<IRateLimitingConfig>();
            this.deviceConnectionActor = new Mock<IDeviceConnectionActor>();
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.devices = new Mock<IDevices>();
            this.loopSettings = new Mock<PropertiesLoopSettings>(this.rateLimitingConfig.Object);
            this.updatePropertiesLogic = new Mock<UpdateReportedProperties>(this.logger.Object);
            this.deviceTagLogic = new Mock<SetDeviceTag>(this.devices.Object, this.logger.Object);

            this.CreateNewDevicePropertiesActor();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Setup_Called_Twice_Should_Throw_Already_Initialized_Exception()
        {
            // Arrange
            this.CreateNewDevicePropertiesActor();

            // Act
            this.SetupDevicePropertiesActor();

            // Assert
            Assert.Throws<DeviceActorAlreadyInitializedException>(
                () => this.SetupDevicePropertiesActor());
        }


        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Handle_Event_Should_Throw_When_Out_Of_Range()
        {
            // Arrange
            const ActorEvents OUT_OF_RANGE_EVENT = (ActorEvents) 123;
            this.CreateNewDevicePropertiesActor();

            // Act
            this.SetupDevicePropertiesActor();

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => this.target.HandleEvent(OUT_OF_RANGE_EVENT));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Should_Return_CountOfFailedTwinUpdates_When_TwinUpdateFails()
        {
            // Arrange
            const int FAILED_DEVICE_TWIN_UPDATES_COUNT = 3;
            this.CreateNewDevicePropertiesActor();
            this.SetupDevicePropertiesActor();
            this.SetupRateLimitingConfig();
            this.loopSettings.Object.NewLoop(); // resets SchedulableTaggings

            // The constructor should initialize count to zero.
            Assert.Equal(0, this.target.FailedTwinUpdatesCount);

            ActorEvents deviceTwinTaggingFailed = ActorEvents.DeviceTaggingFailed;

            // Act
            for (int i = 0; i < FAILED_DEVICE_TWIN_UPDATES_COUNT; i++)
            {
                this.target.HandleEvent(deviceTwinTaggingFailed);
            }

            long failedTwinUpdateCount = this.target.FailedTwinUpdatesCount;

            // Assert
            Assert.Equal(FAILED_DEVICE_TWIN_UPDATES_COUNT, failedTwinUpdateCount);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Should_ReturnZeroForFailedTwinUpdates_When_Started()
        {
            // Arrange
            this.CreateNewDevicePropertiesActor();

            // Act
            long failedTwinUpdateCount = this.target.FailedTwinUpdatesCount;

            // Assert
            Assert.Equal(0, failedTwinUpdateCount);
        }

        private void CreateNewDevicePropertiesActor()
        {
            this.target = new DevicePropertiesActor(
                this.logger.Object,
                this.actorsLogger.Object,
                this.rateLimiting.Object,
                this.updatePropertiesLogic.Object,
                this.deviceTagLogic.Object);
        }

        private void SetupDevicePropertiesActor()
        {
            this.target.Setup(DEVICE_ID,
                this.deviceStateActor.Object,
                this.deviceConnectionActor.Object,
                this.loopSettings.Object);
        }

        private void SetupRateLimitingConfig()
        {
            this.rateLimitingConfig.SetupGet(x => x.TwinWritesPerSecond).Returns(TWIN_WRITES_PER_SECOND);
        }
    }
}
