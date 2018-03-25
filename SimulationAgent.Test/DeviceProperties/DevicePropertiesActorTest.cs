// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
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
        private readonly Mock<IDevicePropertiesLogic> updatePropertiesLogic;
        private readonly Mock<IDeviceConnectionActor> deviceConnectionActor;
        private readonly Mock<IDeviceStateActor> deviceStateActor;

        private DevicePropertiesActor target;

        public DevicePropertiesActorTest(ITestOutputHelper log)
        {
            this.logger = new Mock<ILogger>();
            this.actorsLogger = new Mock<IActorsLogger>();

            this.rateLimiting = new Mock<IRateLimiting>();

            this.deviceConnectionActor = new Mock<IDeviceConnectionActor>();
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.updatePropertiesLogic = new Mock<IDevicePropertiesLogic>();

            this.CreateNewDevicePropertiesActor();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Setup_Called_Twice_Should_Throw_Already_Initialized_Exception()
        {
            // Arrange
            CreateNewDevicePropertiesActor();

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
            CreateNewDevicePropertiesActor();

            // Act
            this.SetupDevicePropertiesActor();

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => this.target.HandleEvent(OUT_OF_RANGE_EVENT));
        }

        private void CreateNewDevicePropertiesActor()
        {
            this.target = new DevicePropertiesActor(
                this.logger.Object,
                this.actorsLogger.Object,
                this.rateLimiting.Object,
                this.updatePropertiesLogic.Object);
        }

        private void SetupDevicePropertiesActor()
        {
            string DEVICE_ID = "01";

            this.target.Setup(DEVICE_ID,
                this.deviceStateActor.Object,
                this.deviceConnectionActor.Object);
        }
    }
}
