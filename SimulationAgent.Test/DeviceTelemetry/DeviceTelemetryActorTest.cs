// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;
using Moq;
using SimulationAgent.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace SimulationAgent.Test.DeviceTelemetry
{
    public class DeviceTelemetryActorTest
    {
        private readonly Mock<ILogger> logger;
        private readonly Mock<IActorsLogger> actorLogger;
        private readonly Mock<IRateLimitingConfig> rateLimitingConfig;
        private readonly Mock<IRateLimiting> rateLimiting;
        private readonly Mock<SendTelemetry> sendTelemetryLogic;
        private readonly Mock<IDeviceStateActor> deviceStateActor;
        private readonly Mock<IDeviceConnectionActor> deviceConnectionActor;
        private readonly Mock<IDevices> devices;
        private readonly DeviceTelemetryActor target;

        public DeviceTelemetryActorTest(ITestOutputHelper log)
        {
            this.logger = new Mock<ILogger>();
            this.actorLogger = new Mock<IActorsLogger>();
            this.rateLimitingConfig = new Mock<IRateLimitingConfig>();
            this.rateLimiting = new Mock<IRateLimiting>();
            this.devices = new Mock<IDevices>();
            this.sendTelemetryLogic = new Mock<SendTelemetry>(this.logger.Object);

 
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.deviceConnectionActor = new Mock<IDeviceConnectionActor>();

            this.rateLimitingConfig.Setup(x => x.DeviceMessagesPerSecond).Returns(10);

            this.target = new DeviceTelemetryActor(
                this.logger.Object,
                this.actorLogger.Object,
                this.rateLimiting.Object,
                this.sendTelemetryLogic.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void TheNumberOfFailedMessagesIsZeroAtStart()
        {
            // Arrange
            this.SetupDeviceTelemetryActor();

            // Act
            long failedMessagesCount = this.target.FailedMessagesCount;

            // Assert
            Assert.Equal(0, failedMessagesCount);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfFailedMessages()
        {
            // Arrange
            const int FAILED_MESSAGES_COUNT = 5;
            this.SetupDeviceTelemetryActor();
            DeviceTelemetryActor.ActorEvents messageFailed = DeviceTelemetryActor.ActorEvents.TelemetrySendFailure;

            // Act
            for(int i = 0; i < FAILED_MESSAGES_COUNT; i++)
            {
                this.target.HandleEvent(messageFailed);
            }

            // Get results
            long failedMessagesCount = this.target.FailedMessagesCount;

            // Assert
            Assert.Equal(FAILED_MESSAGES_COUNT, failedMessagesCount);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void TheNumberOfTotalMessagesIsZeroAtStart()
        {
            // Arrange
            this.SetupDeviceTelemetryActor();

            // Act
            long totalMessagesCount = this.target.TotalMessagesCount;

            // Assert
            Assert.Equal(0, totalMessagesCount);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheNumberOfTotalMessages()
        {
            // Arrange
            const int MESSAGES_SENDING_COUNT = 5;
            this.SetupDeviceTelemetryActor();
            DeviceTelemetryActor.ActorEvents sendingMessage = DeviceTelemetryActor.ActorEvents.SendingTelemetry;

            // Act
            for (int i = 0; i < MESSAGES_SENDING_COUNT; i++)
            {
                this.target.HandleEvent(sendingMessage);
            }

            // Get results
            long totalMessagesCount = this.target.TotalMessagesCount;

            // Assert
            Assert.Equal(MESSAGES_SENDING_COUNT, totalMessagesCount);
        }

        private void SetupDeviceTelemetryActor()
        {
            string DEVICE_ID = "01";
            var deviceModel = new DeviceModel { Id = DEVICE_ID };
            var message = new DeviceModel.DeviceModelMessage();
            this.deviceConnectionActor.SetupGet(x => x.Connected).Returns(true);

            this.target.Setup(
                DEVICE_ID,
                deviceModel,
                message,
                this.deviceStateActor.Object,
                this.deviceConnectionActor.Object);
        }
    }
}
