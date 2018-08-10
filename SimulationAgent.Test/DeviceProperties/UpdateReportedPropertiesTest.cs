// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
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
    public class UpdateReportedPropertiesTest
    {
        private const string DEVICE_ID = "01";
        private readonly Mock<ILogger> logger;
        private readonly Mock<IRateLimitingConfig> rateLimitingConfig;
        private readonly Mock<IDevicePropertiesActor> devicePropertiesActor;
        private readonly Mock<IDeviceStateActor> deviceStateActor;
        private readonly Mock<IDeviceConnectionActor> deviceConnectionActor;
        private readonly Mock<ISmartDictionary> properties;
        private readonly Mock<IDeviceClient> client;
        private readonly Mock<PropertiesLoopSettings> loopSettings;

        private readonly UpdateReportedProperties target;

        public UpdateReportedPropertiesTest(ITestOutputHelper log)
        {
            this.logger = new Mock<ILogger>();
            this.rateLimitingConfig = new Mock<IRateLimitingConfig>();
            this.devicePropertiesActor = new Mock<IDevicePropertiesActor>();
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.deviceConnectionActor = new Mock<IDeviceConnectionActor>();
            this.properties = new Mock<ISmartDictionary>();
            this.client = new Mock<IDeviceClient>();
            this.loopSettings = new Mock<PropertiesLoopSettings>(
                this.rateLimitingConfig.Object);

            this.target = new UpdateReportedProperties(this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Device_Offline_Should_Call_Handle_Event_Update_Failed()
        {
            // Arrange
            this.SetupPropertiesActorProperties();
            this.SetupPropertiesActorStateOffline();
            this.SetupPropertiesChangedToTrue();
            this.target.Setup(this.devicePropertiesActor.Object, DEVICE_ID);

            // Act
            this.target.RunAsync().Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.devicePropertiesActor.Verify(x => x.HandleEvent(ActorEvents.PropertiesUpdateFailed));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Device_Online_Should_Call_Update_Properties_Async()
        {
            // Arrange
            this.SetupPropertiesActor();
            this.SetupPropertiesActorProperties();
            this.SetupPropertiesActorStateOnline();
            this.SetupPropertiesChangedToTrue();
            this.SetupClient();
            this.target.Setup(this.devicePropertiesActor.Object, DEVICE_ID);

            // Act
            this.target.RunAsync().Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.devicePropertiesActor.Verify(x => x.Client.UpdatePropertiesAsync(It.IsAny<ISmartDictionary>()));
        }

        private void SetupPropertiesActor()
        {
            this.devicePropertiesActor.Object.Setup(
                DEVICE_ID,
                this.deviceStateActor.Object,
                this.deviceConnectionActor.Object,
                this.loopSettings.Object);
        }

        private void SetupPropertiesActorProperties()
        {
            var properties = new Dictionary<string, object>
            {
                { "testKey1", "testValue1" },
                { "testKey2", "testValue2" }
            };

            this.devicePropertiesActor
                .Setup(x => x.DeviceProperties.GetAll())
                .Returns(properties);

            var smartDictionary = new SmartDictionary(properties);

            this.devicePropertiesActor
                .Setup(x => x.DeviceProperties)
                .Returns(smartDictionary);
        }

        private void SetupPropertiesChangedToTrue()
        {
            this.devicePropertiesActor
                .Setup(x => x.DeviceProperties.Changed)
                .Returns(true);
        }

        private void SetupPropertiesChangedToFalse()
        {
            this.devicePropertiesActor
                .Setup(x => x.DeviceProperties.Changed)
                .Returns(false);
        }

        private void SetupPropertiesActorStateOnline()
        {
            var state = new Dictionary<string, object>
            {
                { "online", true }
            };

            this.devicePropertiesActor
                .Setup(x => x.DeviceState.GetAll())
                .Returns(state);
        }

        private void SetupPropertiesActorStateOffline()
        {
            var state = new Dictionary<string, object>
            {
                { "online", false }
            };

            this.devicePropertiesActor
                .Setup(x => x.DeviceState.GetAll())
                .Returns(state);
        }

        private void SetupClient()
        {
            this.devicePropertiesActor
                .Setup(x => x.Client)
                .Returns(this.client.Object);

            this.devicePropertiesActor
                .Setup(x => x.Client.UpdatePropertiesAsync(It.IsAny<ISmartDictionary>()))
                .Returns(Task.CompletedTask);
        }
    }
}
