// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Moq;
using Newtonsoft.Json;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;
using SimulationModel = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;

namespace Services.Test
{
    public class SimulationsTest
    {
        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper log;

        private const string STORAGE_COLLECTION = "simulations";
        private const string SIMULATION_ID = "1";

        private readonly Mock<IDeviceModels> deviceModels;
        private readonly Mock<IStorageAdapterClient> storage;
        private readonly Mock<IDevices> devices;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IIotHubConnectionStringManager> connStringManager;
        private readonly Simulations target;
        private readonly List<DeviceModel> models;

        public SimulationsTest(ITestOutputHelper log)
        {
            this.log = log;

            this.deviceModels = new Mock<IDeviceModels>();
            this.storage = new Mock<IStorageAdapterClient>();
            this.logger = new Mock<ILogger>();
            this.devices = new Mock<IDevices>();
            this.connStringManager = new Mock<IIotHubConnectionStringManager>();
            this.models = new List<DeviceModel>
            {
                new DeviceModel { Id = "01" },
                new DeviceModel { Id = "05" },
                new DeviceModel { Id = "02" },
                new DeviceModel { Id = "AA" }
            };

            this.target = new Simulations(this.deviceModels.Object, this.storage.Object, this.connStringManager.Object, this.devices.Object, this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void InitialListIsEmpty()
        {
            // Arrange
            this.ThereAreNoSimulationsInTheStorage();

            // Act
            var list = this.target.GetListAsync().Result;

            // Assert
            Assert.Equal(0, list.Count);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void InitialMetadataAfterCreation()
        {
            // Arrange
            this.ThereAreNoSimulationsInTheStorage();
            this.ThereAreSomeDeviceModels();

            // Act
            SimulationModel result = this.target.InsertAsync(new SimulationModel(), "default").Result;

            // Assert
            Assert.Equal(1, result.Version);
            Assert.Equal(result.Created, result.Modified);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreateDefaultSimulation()
        {
            // Arrange
            const int DEFAULT_DEVICE_COUNT = 1;
            this.ThereAreNoSimulationsInTheStorage();
            this.ThereAreSomeDeviceModels();

            // Act
            SimulationModel result = this.target.InsertAsync(new SimulationModel(), "default").Result;

            // Assert
            Assert.Equal(this.models.Count, result.DeviceModels.Count);
            for (var i = 0; i < this.models.Count; i++)
            {
                Assert.Equal(this.models[i].Id, result.DeviceModels[i].Id);
                Assert.Equal(DEFAULT_DEVICE_COUNT, result.DeviceModels[i].Count);
            }
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreateSimulationWithoutId()
        {
            // Arrange
            this.ThereAreNoSimulationsInTheStorage();
            this.ThereAreSomeDeviceModels();

            // Act
            SimulationModel result = this.target.InsertAsync(new SimulationModel(), "default").Result;

            // Assert
            Assert.NotEmpty(result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreateSimulationWithId()
        {
            // Arrange
            this.ThereAreSomeDeviceModels();
            this.ThereAreNoSimulationsInTheStorage();

            // Act
            var simulation = new SimulationModel { Id = "123" };
            SimulationModel result = this.target.InsertAsync(simulation, "default").Result;

            // Assert
            Assert.Equal(simulation.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreateWithInvalidTemplate()
        {
            // Act + Assert
            Assert.ThrowsAsync<InvalidInputException>(
                () => this.target.InsertAsync(new SimulationModel(), "mytemplate"));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreatingMultipleSimulationsIsNotAllowed()
        {
            // Arrange
            this.ThereAreSomeDeviceModels();
            this.ThereIsAnEnabledSimulationInTheStorage();

            // Act + Assert
            var s = new SimulationModel { Id = Guid.NewGuid().ToString(), Enabled = false };
            Assert.ThrowsAsync<ConflictingResourceException>(() => this.target.InsertAsync(s));
            Assert.ThrowsAsync<ConflictingResourceException>(() => this.target.UpsertAsync(s));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreatedSimulationsAreStored()
        {
            // Arrange
            this.ThereAreSomeDeviceModels();
            this.ThereAreNoSimulationsInTheStorage();

            // Act
            var simulation = new SimulationModel { Id = Guid.NewGuid().ToString(), Enabled = false };
            this.target.InsertAsync(simulation, "default").Wait();

            // Assert
            this.storage.Verify(
                x => x.UpdateAsync(STORAGE_COLLECTION, SIMULATION_ID, It.IsAny<string>(), "*"));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void SimulationsCanBeUpserted()
        {
            // Arrange
            this.ThereAreSomeDeviceModels();
            this.ThereAreNoSimulationsInTheStorage();
            // Arrange the simulation data returned by the storage adapter
            var updatedValue = new ValueApiModel { ETag = "newETag" };
            this.storage.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(updatedValue);

            // Act
            var simulation = new SimulationModel
            {
                Id = SIMULATION_ID,
                Enabled = false,
                ETag = "oldETag"
            };
            this.target.UpsertAsync(simulation).Wait();

            // Assert
            this.storage.Verify(
                x => x.UpdateAsync(STORAGE_COLLECTION, SIMULATION_ID, It.IsAny<string>(), "oldETag"));
            Assert.Equal("newETag", simulation.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void UpsertRequiresIdWhileInsertDoesNot()
        {
            // Act
            var s1 = new SimulationModel();
            var s2 = new SimulationModel();
            this.target.InsertAsync(s1);

            // Act + Assert
            Assert.ThrowsAsync<InvalidInputException>(() => this.target.UpsertAsync(s2));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void UpsertUsesOptimisticConcurrency()
        {
            // Arrange
            this.deviceModels.Setup(x => x.GetList()).Returns(this.models);

            var id = Guid.NewGuid().ToString();
            var s1 = new SimulationModel { Id = id, Enabled = false };
            this.target.UpsertAsync(s1);

            // Act + Assert
            var s1Updated = new SimulationModel { Id = id, Enabled = true };
            Assert.ThrowsAsync<ResourceOutOfDateException>(() => this.target.UpsertAsync(s1Updated));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ThereAreNoNullPropertiesInTheDeviceModel()
        {
            // Arrange
            this.ThereAreSomeDeviceModels();
            this.ThereAreNoSimulationsInTheStorage();

            // Arrange the simulation data returned by the storage adapter
            var simulation = new SimulationModel
            {
                Id = SIMULATION_ID,
                ETag = "ETag0",
                Enabled = true,
                Version = 1
            };
            var updatedValue = new ValueApiModel
            {
                Key = SIMULATION_ID,
                Data = JsonConvert.SerializeObject(simulation),
                ETag = simulation.ETag
            };
            this.storage.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(updatedValue);

            // Act
            this.target.UpsertAsync(simulation).Wait();

            // Assert
            this.storage.Verify(x => x.UpdateAsync(
                STORAGE_COLLECTION,
                SIMULATION_ID,
                It.Is<string>(s => !s.Contains("null")),
                "ETag0"));
        }

        private void ThereAreSomeDeviceModels()
        {
            this.deviceModels.Setup(x => x.GetList()).Returns(this.models);
        }

        private void ThereAreNoSimulationsInTheStorage()
        {
            this.storage.Setup(x => x.GetAllAsync(STORAGE_COLLECTION)).ReturnsAsync(new ValueListApiModel());
            // In case the test inserts a record, return a valid storage object
            this.storage.Setup(x => x.UpdateAsync(STORAGE_COLLECTION, SIMULATION_ID, It.IsAny<string>(), "*"))
                .ReturnsAsync(new ValueApiModel { Key = SIMULATION_ID, Data = "{}", ETag = "someETag" });
        }

        private void ThereIsAnEnabledSimulationInTheStorage()
        {
            var simulation = new SimulationModel
            {
                Id = SIMULATION_ID,
                Created = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                Modified = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                ETag = "ETag0",
                Enabled = true,
                Version = 1
            };

            var list = new ValueListApiModel();
            var value = new ValueApiModel
            {
                Key = SIMULATION_ID,
                Data = JsonConvert.SerializeObject(simulation),
                ETag = simulation.ETag
            };
            list.Items.Add(value);

            this.storage.Setup(x => x.GetAllAsync(STORAGE_COLLECTION)).ReturnsAsync(list);
        }
    }
}
