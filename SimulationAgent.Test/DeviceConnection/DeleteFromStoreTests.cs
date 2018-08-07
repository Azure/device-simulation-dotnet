// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Moq;
using SimulationAgent.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace SimulationAgent.Test.DeviceConnection
{
    public class DeleteFromStoreTest
    {
        private const string DEVICE_ID = "01";
        private const string DEVICES_COLLECTION = "SimulatedDevices";

        private readonly Mock<ILogger> logger;
        private Mock<IDevices> devices;
        private Mock<IStorageAdapterClient> storage;
        private readonly Mock<IScriptInterpreter> scriptInterpreter;
        private readonly Mock<IDeviceStateActor> deviceStateActor;
        private readonly Mock<IDeviceConnectionActor> deviceConnectionActor;
        private readonly Mock<IRateLimitingConfig> rateLimitingConfig;
        private readonly Mock<ConnectionLoopSettings> loopSettings;

        private DeviceModel deviceModel;
        private DeleteFromStore target;

        public DeleteFromStoreTest(ITestOutputHelper log)
        {
            this.logger = new Mock<ILogger>();
            this.devices = new Mock<IDevices>();
            this.storage = new Mock<IStorageAdapterClient>();
            this.scriptInterpreter = new Mock<IScriptInterpreter>();
            this.rateLimitingConfig = new Mock<IRateLimitingConfig>();
            this.deviceStateActor = new Mock<IDeviceStateActor>();
            this.deviceConnectionActor = new Mock<IDeviceConnectionActor>();
            this.loopSettings = new Mock<ConnectionLoopSettings>(this.rateLimitingConfig.Object);
            this.deviceModel = new DeviceModel { Id = DEVICE_ID };

            this.target = new DeleteFromStore(this.storage.Object, this.devices.Object, this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task ItSetsStateToAddToStoreCompleted()
        {
            // Arrange
            this.SetupDeviceConnectionActor();
            this.target.Setup(this.deviceConnectionActor.Object, DEVICE_ID, this.deviceModel);

            // Act
            await this.target.RunAsync();

            // Assert
            this.storage.Verify(m => m.DeleteAsync(DEVICES_COLLECTION, DEVICE_ID));
            this.deviceConnectionActor.Verify(x => x.HandleEvent(DeviceConnectionActor.ActorEvents.DeleteFromStoreCompleted));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task ItSetsStateToAddToStoreFailed()
        {
            // Arrange
            this.SetupDeviceConnectionActor();
            this.target.Setup(this.deviceConnectionActor.Object, DEVICE_ID, this.deviceModel);
            this.storage.Setup(x => x.DeleteAsync(DEVICES_COLLECTION, It.IsAny<string>())).Throws<Exception>();

            // Act
            await this.target.RunAsync();

            // Assert
            this.deviceConnectionActor.Verify(x => x.HandleEvent(DeviceConnectionActor.ActorEvents.DeleteFromStoreFailed));
        }

        private void SetupDeviceConnectionActor()
        {
            this.deviceConnectionActor.Object.Setup(
                DEVICE_ID,
                this.deviceModel,
                this.deviceStateActor.Object,
                this.loopSettings.Object);
        }
    }
}
