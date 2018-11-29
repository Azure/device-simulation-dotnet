// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
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
        private readonly Mock<CredentialsSetup> credentialSetup;
        private readonly Mock<IRateLimiting> mockRateLimiting;
        private readonly Mock<IRateLimitingConfig> rateLimitingConfig;
        private readonly Mock<IDevices> devices;
        private readonly Mock<IStorageAdapterClient> storageAdapterClient;
        private readonly Mock<UpdateReportedProperties> updatePropertiesLogic;
        private readonly Mock<SetDeviceTag> deviceTagLogic;
        private readonly Mock<IDeviceConnectionActor> mockDeviceContext;
        private readonly Mock<IDeviceStateActor> deviceStateActor;
        private readonly Mock<PropertiesLoopSettings> loopSettings;
        private readonly Mock<IInstance> mockInstance;

        private const string DEVICE_ID = "01";
        private const int TWIN_WRITES_PER_SECOND = 10;
        private bool isInstanceInitialized;

        private DevicePropertiesActor target;

        public DevicePropertiesActorTest(ITestOutputHelper log)
        {
            this.logger = new Mock<ILogger>();
            this.actorsLogger = new Mock<IActorsLogger>();
            this.mockRateLimiting = new Mock<IRateLimiting>();
            this.credentialSetup = new Mock<CredentialsSetup>();
            this.rateLimitingConfig = new Mock<IRateLimitingConfig>();
            this.mockDeviceContext = new Mock<IDeviceConnectionActor>();
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.devices = new Mock<IDevices>();
            this.loopSettings = new Mock<PropertiesLoopSettings>(this.rateLimitingConfig.Object);
            this.updatePropertiesLogic = new Mock<UpdateReportedProperties>(this.logger.Object);
            this.storageAdapterClient = new Mock<IStorageAdapterClient>();
            this.mockInstance = new Mock<IInstance>();
            this.deviceTagLogic = new Mock<SetDeviceTag>(this.logger.Object, this.mockInstance.Object);
            this.isInstanceInitialized = false;

            this.CreateNewDevicePropertiesActor();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Init_Called_Twice_Should_Throw_Already_Initialized_Exception()
        {
            // Arrange
            this.CreateNewDevicePropertiesActor();

            // Act
            this.SetupDevicePropertiesActor();

            // Assert
            Assert.Throws<ApplicationException>(
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
        public void It_Should_Return_CountOfFailedTwinUpdates_When_TwinUpdateFails()
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
            // Set up the mock Instance to throw an exception if
            // the instance is initialized more than once.
            this.isInstanceInitialized = false;
            this.mockInstance.Setup(
                    x => x.InitOnce(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<int>()))
                .Callback(() =>
                {
                    if (this.isInstanceInitialized)
                        throw new ApplicationException("Multiple initializations attempted.");
                    this.isInstanceInitialized = true;
                });

            this.target = new DevicePropertiesActor(
                this.logger.Object,
                this.actorsLogger.Object,
                this.updatePropertiesLogic.Object,
                this.deviceTagLogic.Object,
                this.mockInstance.Object);
        }

        private void SetupDevicePropertiesActor()
        {
            // Setup a SimulationContext object
            var testSimulation = new Simulation();
            var mockSimulationContext = new Mock<ISimulationContext>();
            mockSimulationContext.Object.InitAsync(testSimulation).Wait(Constants.TEST_TIMEOUT);
            mockSimulationContext.SetupGet(x => x.Devices).Returns(this.devices.Object);
            mockSimulationContext.SetupGet(x => x.RateLimiting).Returns(this.mockRateLimiting.Object);

            this.target.Init(
                mockSimulationContext.Object,
                DEVICE_ID,
                this.deviceStateActor.Object,
                this.mockDeviceContext.Object,
                this.loopSettings.Object);
        }

        private void SetupRateLimitingConfig()
        {
            this.rateLimitingConfig.SetupGet(x => x.TwinWritesPerSecond).Returns(TWIN_WRITES_PER_SECOND);
        }

        private DeviceConnectionActor GetDeviceConnectionActor()
        {
            Mock<IScriptInterpreter> scriptInterpreter = new Mock<IScriptInterpreter>();
            Mock<FetchFromRegistry> fetchLogic = new Mock<FetchFromRegistry>(
                this.devices.Object,
                this.logger.Object);
            Mock<Register> registerLogic = new Mock<Register>(
                this.devices.Object,
                this.logger.Object);
            Mock<Connect> connectLogic = new Mock<Connect>(
                this.devices.Object,
                scriptInterpreter.Object,
                this.logger.Object);
            Mock<Deregister> deregisterLogic = new Mock<Deregister>(
                this.devices.Object,
                this.logger.Object);
            Mock<Disconnect> disconnectLogic = new Mock<Disconnect>(
                this.devices.Object,
                scriptInterpreter.Object,
                this.logger.Object);

            return new DeviceConnectionActor(
                this.logger.Object,
                this.actorsLogger.Object,
                this.credentialSetup.Object,
                fetchLogic.Object,
                registerLogic.Object,
                connectLogic.Object,
                deregisterLogic.Object,
                disconnectLogic.Object,
                this.mockInstance.Object);
        }
    }
}
