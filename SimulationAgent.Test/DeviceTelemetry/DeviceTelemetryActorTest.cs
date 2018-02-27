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
        public void ReportMetricsBeforeRun()
        {
            // Arrange
            SetupDeviceTelemetryActor();

            // Act
            int failedMessagesCount = this.target.FailedMessagesCount;
            int totalMessagesCount = this.target.TotalMessagesCount;

            // Assert
            Assert.Equal(0, failedMessagesCount);
            Assert.Equal(0, totalMessagesCount);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReportMetricsAfterRun()
        {
            // Arrange
            SetupDeviceTelemetryActor();
            DeviceTelemetryActor.ActorEvents messageDelivered = DeviceTelemetryActor.ActorEvents.TelemetryDelivered;
            DeviceTelemetryActor.ActorEvents messageFailed = DeviceTelemetryActor.ActorEvents.TelemetryDeliveryFailed;

            // Act
            // Deliver 5 messages
            for (int i = 0; i < 5; i++)
            {
                this.target.HandleEvent(messageDelivered);
            }
            // One failed message
            this.target.HandleEvent(messageFailed);
            // Get results
            int failedMessagesCount = this.target.FailedMessagesCount;
            int totalMessagesCount = this.target.TotalMessagesCount;

            // Assert
            Assert.Equal(1, failedMessagesCount);
            Assert.Equal(6, totalMessagesCount);
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
