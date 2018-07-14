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

namespace SimulationAgent.Test.DeviceConnection
{
    public class DeviceConnectionActorTest
    {
        private readonly Mock<ILogger> logger;
        private readonly Mock<IActorsLogger> actorLogger;
        private readonly Mock<IRateLimitingConfig> rateLimitingConfig;
        private readonly Mock<IScriptInterpreter> scriptInterpreter;
        private readonly Mock<IRateLimiting> rateLimiting;
        private readonly Mock<CredentialsSetup> credentialsSetupLogic;
        private readonly Mock<FetchFromRegistry> fetchLogic;
        private readonly Mock<Register> registerLogic;
        private readonly Mock<IDevices> devices;
        private readonly Mock<Connect> connectLogic;
        private readonly Mock<IDeviceStateActor> deviceStateActor;
        private readonly Mock<ConnectionLoopSettings> loopSettings;
        private readonly DeviceConnectionActor target;

        public DeviceConnectionActorTest()
        {
            this.logger = new Mock<ILogger>();
            this.actorLogger = new Mock<IActorsLogger>();
            this.rateLimitingConfig = new Mock<IRateLimitingConfig>();
            this.scriptInterpreter = new Mock<IScriptInterpreter>();
            this.devices = new Mock<IDevices>();
            this.rateLimiting = new Mock<IRateLimiting>();
            this.credentialsSetupLogic = new Mock<CredentialsSetup>(
                this.devices.Object,
                this.logger.Object);
            this.fetchLogic = new Mock<FetchFromRegistry>(
                this.devices.Object,
                this.logger.Object);
            this.registerLogic = new Mock<Register>(
                this.devices.Object,
                this.logger.Object);
            this.connectLogic = new Mock<Connect>(
                this.devices.Object,
                this.scriptInterpreter.Object,
                this.logger.Object);
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.loopSettings = new Mock<ConnectionLoopSettings>(
                this.rateLimitingConfig.Object);

            this.rateLimitingConfig.Setup(x => x.DeviceMessagesPerSecond).Returns(10);

            this.target = new DeviceConnectionActor(
                this.logger.Object,
                this.actorLogger.Object,
                this.rateLimiting.Object,
                this.credentialsSetupLogic.Object,
                this.fetchLogic.Object,
                this.registerLogic.Object,
                this.connectLogic.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void TheNumberOfFailedDeviceConnectionsIsZeroAtStart()
        {
            // Arrange
            this.SetupDeviceConnectionActor();

            // Act
            long failedDeviceConnectionCount = this.target.FailedDeviceConnectionsCount;

            // Assert
            Assert.Equal(0, failedDeviceConnectionCount);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfFailedDeviceConnections()
        {
            // Arrange
            const int FAILED_DEVICE_CONNECTIONS_COUNT = 3;
            this.SetupDeviceConnectionActor();
            DeviceConnectionActor.ActorEvents connectionFailed = DeviceConnectionActor.ActorEvents.ConnectionFailed;

            // Act
            for (int i = 0; i < FAILED_DEVICE_CONNECTIONS_COUNT; i++)
            {
                this.target.HandleEvent(connectionFailed);
            }

            long failedDeviceConnectionCount = this.target.FailedDeviceConnectionsCount;

            // Assert
            Assert.Equal(FAILED_DEVICE_CONNECTIONS_COUNT, failedDeviceConnectionCount);
        }

        private void SetupDeviceConnectionActor()
        {
            string DEVICE_ID = "01";
            var deviceModel = new DeviceModel { Id = DEVICE_ID };

            this.SetupRateLimitingConfig();

            this.target.Setup(
                DEVICE_ID,
                deviceModel,
                this.deviceStateActor.Object,
                this.loopSettings.Object);
        }

        private void SetupRateLimitingConfig()
        {
            this.rateLimitingConfig
                .SetupGet(x => x.RegistryOperationsPerMinute)
                .Returns(1200);
        }
    }
}
