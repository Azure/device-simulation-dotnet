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
using SimulationModel = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;

namespace Services.Test
{
    public class SimulationsTest
    {
        private const string STORAGE_COLLECTION = "simulations";
        private const string SIMULATION_ID = "1";

        private readonly Simulations target;
        private readonly List<DeviceModel> models;

        private readonly Mock<IDeviceModels> deviceModels;
        private readonly Mock<IStorageAdapterClient> storage;
        private readonly Mock<IDevices> devices;
        private readonly Mock<IDiagnosticsLogger> diagnosticsLogger;
        private readonly Mock<IIotHubConnectionStringManager> connStringManager;
        private readonly Mock<ILogger> log;

        public SimulationsTest()
        {
            this.deviceModels = new Mock<IDeviceModels>();
            this.storage = new Mock<IStorageAdapterClient>();
            this.log = new Mock<ILogger>();
            this.diagnosticsLogger = new Mock<IDiagnosticsLogger>();
            this.devices = new Mock<IDevices>();
            this.connStringManager = new Mock<IIotHubConnectionStringManager>();
            this.models = new List<DeviceModel>
            {
                new DeviceModel { Id = "01" },
                new DeviceModel { Id = "05" },
                new DeviceModel { Id = "02" },
                new DeviceModel { Id = "AA" }
            };

            this.target = new Simulations(
                this.deviceModels.Object,
                this.storage.Object,
                this.connStringManager.Object,
                this.devices.Object,
                this.log.Object,
                this.diagnosticsLogger.Object);
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
            Assert.False(result.PartitioningComplete);
            // Note: the 2 dates are not set at the same time, we just check that they're close enough
            Assert.True(Math.Abs(result.Modified.ToUnixTimeSeconds() - result.Created.ToUnixTimeSeconds()) < 10);
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
            Assert.False(result.PartitioningComplete);
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
                    async () => await this.target.InsertAsync(new SimulationModel(), "mytemplate"))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreatedSimulationsAreStored()
        {
            // Arrange
            this.ThereAreSomeDeviceModels();
            this.ThereAreNoSimulationsInTheStorage();

            // Act
            var simulation = new SimulationModel
            {
                Id = Guid.NewGuid().ToString(),
                Enabled = false,
                Description = "some-description"
            };
            this.target.InsertAsync(simulation, "default").Wait();

            // Assert
            this.storage.Verify(
                x => x.UpdateAsync(STORAGE_COLLECTION, SIMULATION_ID, It.Is<string>(
                    s => !JsonConvert.DeserializeObject<SimulationModel>(s).Enabled
                ), "*"), Times.Once);
            this.storage.Verify(
                x => x.UpdateAsync(STORAGE_COLLECTION, SIMULATION_ID, It.Is<string>(
                    s => JsonConvert.DeserializeObject<SimulationModel>(s).Description == "some-description"
                ), "*"), Times.Once);
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
                x => x.UpdateAsync(STORAGE_COLLECTION, SIMULATION_ID, It.IsAny<string>(), "oldETag"),
                Times.Once);
            Assert.Equal("newETag", simulation.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void UpsertRequiresIdWhileInsertDoesNot()
        {
            // Arrange
            var s1 = new SimulationModel { Name = "Test Simulation 1" };
            var s2 = new SimulationModel { Name = "Test Simulation 2" };
            this.ThereAreNoSimulationsInTheStorage();

            // Act - No exception occurs
            this.target.InsertAsync(s1).Wait(Constants.TEST_TIMEOUT);

            // Act + Assert
            Assert.ThrowsAsync<InvalidInputException>(async () => await this.target.UpsertAsync(s2))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void UpsertUsesOptimisticConcurrency()
        {
            // Arrange
            const string ETAG1 = "001";
            const string ETAG2 = "002";

            // Initial simulation 
            var simulation1 = new SimulationModel { Id = SIMULATION_ID, Name = "Test Simulation 1", ETag = ETAG1 };
            var storageRecord1 = new ValueApiModel
            {
                Key = SIMULATION_ID,
                Data = JsonConvert.SerializeObject(simulation1),
                ETag = simulation1.ETag
            };
            var storageList1 = new ValueListApiModel();
            storageList1.Items.Add(storageRecord1);

            // Simulation after update
            var simulation2 = new SimulationModel { Id = SIMULATION_ID, Name = "Test Simulation 2", ETag = ETAG2 };
            var storageRecord2 = new ValueApiModel
            {
                Key = SIMULATION_ID,
                Data = JsonConvert.SerializeObject(simulation2),
                ETag = simulation2.ETag
            };
            var storageList2 = new ValueListApiModel();
            storageList2.Items.Add(storageRecord2);

            // Initial setup - the ETag matches
            this.storage.Setup(x => x.GetAllAsync(STORAGE_COLLECTION)).ReturnsAsync(storageList1);
            this.storage.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(storageRecord2);

            // Act - No exception because ETag matches
            // Note: the call to UpsertAsync modifies the object, don't reuse the variable later
            this.target.UpsertAsync(simulation1).Wait(Constants.TEST_TIMEOUT);

            // Arrange - the ETag won't match
            this.storage.Setup(x => x.GetAllAsync(STORAGE_COLLECTION)).ReturnsAsync(storageList2);
            this.storage.Setup(x => x.GetAsync(STORAGE_COLLECTION, SIMULATION_ID)).ReturnsAsync(storageRecord2);

            // Act + Assert
            var simulationOutOfDate = new SimulationModel { Id = SIMULATION_ID, ETag = ETAG1 };

            Assert.ThrowsAsync<ResourceOutOfDateException>(
                    async () => await this.target.UpsertAsync(simulationOutOfDate))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ThereAreNoNullPropertiesInTheDeviceModel()
        {
            // Arrange
            this.ThereAreSomeDeviceModels();
            this.ThereAreNoSimulationsInTheStorage();

            // Arrange the simulation data returned by the storage adapter
            var id = SIMULATION_ID;
            var simulation = new SimulationModel
            {
                Id = id,
                Name = "Test Simulation",
                ETag = "ETag0",
                Enabled = true
            };
            var updatedValue = new ValueApiModel
            {
                Key = id,
                Data = JsonConvert.SerializeObject(simulation),
                ETag = simulation.ETag
            };
            this.storage.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(updatedValue);

            // Act
            this.target.UpsertAsync(simulation).Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.storage.Verify(x => x.UpdateAsync(
                    STORAGE_COLLECTION, id, It.IsAny<string>(), "ETag0"),
                Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntAllowUsersToOverwritePartitioningStatus()
        {
            // Arrange
            var sim = new SimulationModel
            {
                Id = "1",
                Enabled = true,
                PartitioningComplete = false
            };
            var record = new ValueApiModel
            {
                Key = "1",
                Data = JsonConvert.SerializeObject(sim)
            };
            this.storage.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(record);
            this.storage.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(record);

            // Act
            var update = new SimulationModel
            {
                Id = sim.Id,
                Enabled = true,
                PartitioningComplete = true,
                ETag = "*"
            };
            SimulationModel result = this.target.UpsertAsync(update).CompleteOrTimeout().Result;

            // Assert
            Assert.False(result.PartitioningComplete);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesNewSimulationsWithPartitioningStateNotComplete()
        {
            // Arrange
            this.storage.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((ValueApiModel) null);
            var sim = new SimulationModel
            {
                Id = "1",
                Enabled = true,
                PartitioningComplete = true
            };
            var record = new ValueApiModel
            {
                Key = "1",
                Data = JsonConvert.SerializeObject(sim),
            };
            this.storage.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(record);

            // Act
            SimulationModel result = this.target.UpsertAsync(sim).CompleteOrTimeout().Result;

            // Assert
            Assert.False(result.PartitioningComplete);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItTriggersPartitionsDeletionWhenASimulationIsDisabled()
        {
            // Arrange
            var sim = new SimulationModel
            {
                Id = "1",
                Enabled = true,
                PartitioningComplete = true
            };
            var record = new ValueApiModel
            {
                Key = "1",
                Data = JsonConvert.SerializeObject(sim)
            };
            this.storage.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(record);
            this.storage.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(record);

            // Act
            var update = new SimulationModel
            {
                Id = sim.Id,
                Enabled = false,
                PartitioningComplete = true,
                ETag = "*"
            };
            SimulationModel result = this.target.UpsertAsync(update).CompleteOrTimeout().Result;

            // Assert
            Assert.False(result.Enabled);
            Assert.False(result.PartitioningComplete);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItGenerateDevicesIdGroupedByModel()
        {
            // Arrange
            var sim = new SimulationModel
            {
                Id = "1",
                Enabled = true,
                PartitioningComplete = true,
                DeviceModels = new List<SimulationModel.DeviceModelRef>
                {
                    new SimulationModel.DeviceModelRef { Id = "modelA", Count = 5 },
                    new SimulationModel.DeviceModelRef { Id = "modelB", Count = 2 },
                    new SimulationModel.DeviceModelRef { Id = "modelC", Count = 4 }
                }
            };

            // Act
            Dictionary<string, List<string>> result = this.target.GetDeviceIdsByModel(sim);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.True(result.ContainsKey("modelA"));
            Assert.True(result.ContainsKey("modelB"));
            Assert.True(result.ContainsKey("modelC"));
            Assert.Equal(5, result["modelA"].Count);
            Assert.Equal(2, result["modelB"].Count);
            Assert.Equal(4, result["modelC"].Count);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItGeneratesDeviceIdsFollowingAKnownFormat()
        {
            // Arrange
            var simulationId = Guid.NewGuid().ToString();
            var modelId1 = Guid.NewGuid().ToString();
            var modelId2 = Guid.NewGuid().ToString();
            var sim = new SimulationModel
            {
                Id = simulationId,
                Enabled = true,
                PartitioningComplete = true,
                DeviceModels = new List<SimulationModel.DeviceModelRef>
                {
                    new SimulationModel.DeviceModelRef { Id = modelId1, Count = 3 },
                    new SimulationModel.DeviceModelRef { Id = modelId2, Count = 2 }
                }
            };

            // Act
            Dictionary<string, List<string>> result = this.target.GetDeviceIdsByModel(sim);

            // Assert
            Assert.True(result.ContainsKey(modelId1));
            Assert.True(result.ContainsKey(modelId2));

            Assert.Contains($"{simulationId}.{modelId1}.1", result[modelId1]);
            Assert.Contains($"{simulationId}.{modelId1}.2", result[modelId1]);
            Assert.Contains($"{simulationId}.{modelId1}.3", result[modelId1]);

            Assert.Contains($"{simulationId}.{modelId2}.1", result[modelId2]);
            Assert.Contains($"{simulationId}.{modelId2}.2", result[modelId2]);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItIncludesCustomDevicesWhenGeneratingTheListOfDevices()
        {
            // Arrange
            var simulationId = Guid.NewGuid().ToString();
            var modelId1 = Guid.NewGuid().ToString();
            var modelId2 = Guid.NewGuid().ToString();
            var modelId3 = Guid.NewGuid().ToString();
            var modelId4 = Guid.NewGuid().ToString();
            var custom1 = Guid.NewGuid().ToString();
            var custom2 = Guid.NewGuid().ToString();
            var custom3 = Guid.NewGuid().ToString();
            var custom4 = Guid.NewGuid().ToString();
            var custom5 = Guid.NewGuid().ToString();
            var sim = new SimulationModel
            {
                Id = simulationId,
                Enabled = true,
                PartitioningComplete = true,
                DeviceModels = new List<SimulationModel.DeviceModelRef>
                {
                    new SimulationModel.DeviceModelRef { Id = modelId1, Count = 3 },
                    new SimulationModel.DeviceModelRef { Id = modelId2, Count = 2 }
                },
                CustomDevices = new List<SimulationModel.CustomDeviceRef>
                {
                    new SimulationModel.CustomDeviceRef
                    {
                        DeviceId = custom1,
                        DeviceModel = new SimulationModel.DeviceModelRef { Id = modelId1 }
                    },
                    new SimulationModel.CustomDeviceRef
                    {
                        DeviceId = custom2,
                        DeviceModel = new SimulationModel.DeviceModelRef { Id = modelId1 }
                    },
                    new SimulationModel.CustomDeviceRef
                    {
                        DeviceId = custom3,
                        DeviceModel = new SimulationModel.DeviceModelRef { Id = modelId3 }
                    },
                    new SimulationModel.CustomDeviceRef
                    {
                        DeviceId = custom4,
                        DeviceModel = new SimulationModel.DeviceModelRef { Id = modelId3 }
                    },
                    new SimulationModel.CustomDeviceRef
                    {
                        DeviceId = custom5,
                        DeviceModel = new SimulationModel.DeviceModelRef { Id = modelId4 }
                    }
                }
            };

            // Act
            Dictionary<string, List<string>> result = this.target.GetDeviceIdsByModel(sim);

            // Assert
            Assert.Equal(4, result.Count);
            Assert.True(result.ContainsKey(modelId1));
            Assert.True(result.ContainsKey(modelId2));
            Assert.True(result.ContainsKey(modelId3));
            Assert.True(result.ContainsKey(modelId4));

            Assert.Equal(5, result[modelId1].Count);
            Assert.Equal(2, result[modelId2].Count);
            Assert.Equal(2, result[modelId3].Count);
            Assert.Single(result[modelId4]);

            Assert.Contains($"{simulationId}.{modelId1}.1", result[modelId1]);
            Assert.Contains($"{simulationId}.{modelId1}.2", result[modelId1]);
            Assert.Contains($"{simulationId}.{modelId1}.3", result[modelId1]);

            Assert.Contains($"{simulationId}.{modelId2}.1", result[modelId2]);
            Assert.Contains($"{simulationId}.{modelId2}.2", result[modelId2]);

            Assert.Contains(custom1, result[modelId1]);
            Assert.Contains(custom2, result[modelId1]);
            Assert.Contains(custom3, result[modelId3]);
            Assert.Contains(custom4, result[modelId3]);
            Assert.Contains(custom5, result[modelId4]);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItStartsTheDeviceCreationUsingJobs()
        {
            // Arrange
            var simulationId = Guid.NewGuid().ToString();
            var sim = new SimulationModel
            {
                Id = simulationId, Enabled = true, DevicesCreationStarted = false,
                DeviceModels = new List<SimulationModel.DeviceModelRef>
                {
                    new SimulationModel.DeviceModelRef { Id = "some", Count = 3 }
                }
            };
            var record = new ValueApiModel
            {
                Key = simulationId, Data = JsonConvert.SerializeObject(sim)
            };
            this.storage.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(record);

            // Arrange - needed for the record update
            this.storage.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel { Key = SIMULATION_ID, Data = "{}", ETag = "someETag" });

            // Act
            var result = this.target.TryToStartDevicesCreationAsync(simulationId, this.devices.Object)
                .CompleteOrTimeout().Result;

            // Assert
            Assert.True(result);
            this.devices.Verify(x => x.CreateListUsingJobsAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            this.storage.Verify(
                x => x.UpdateAsync(STORAGE_COLLECTION, simulationId, It.Is<string>(
                    s => JsonConvert.DeserializeObject<SimulationModel>(s).DevicesCreationStarted
                ), It.IsAny<string>()), Times.Once());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItStartsTheDeviceCreationOnlyIfNotStarted()
        {
            // Arrange
            var simulationId = Guid.NewGuid().ToString();
            var sim = new SimulationModel
            {
                Id = simulationId, Enabled = true, DevicesCreationStarted = true,
                DeviceModels = new List<SimulationModel.DeviceModelRef>
                {
                    new SimulationModel.DeviceModelRef { Id = "some", Count = 3 }
                }
            };
            var record = new ValueApiModel
            {
                Key = simulationId, Data = JsonConvert.SerializeObject(sim)
            };
            this.storage.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(record);

            // Act
            var result = this.target.TryToStartDevicesCreationAsync(simulationId, this.devices.Object)
                .CompleteOrTimeout().Result;

            // Assert
            Assert.True(result);
            this.devices.Verify(x => x.CreateListUsingJobsAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReportsIfTheDeviceCreationStartFails()
        {
            // Arrange
            var simulationId = Guid.NewGuid().ToString();
            var sim = new SimulationModel
            {
                Id = simulationId, Enabled = true, DevicesCreationStarted = false,
                DeviceModels = new List<SimulationModel.DeviceModelRef>
                {
                    new SimulationModel.DeviceModelRef { Id = "some", Count = 3 }
                }
            };
            var record = new ValueApiModel
            {
                Key = simulationId, Data = JsonConvert.SerializeObject(sim)
            };
            this.storage.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(record);
            this.devices.Setup(x => x.CreateListUsingJobsAsync(It.IsAny<IEnumerable<string>>()))
                .Throws<SomeException>();

            // Act
            var result = this.target.TryToStartDevicesCreationAsync(simulationId, this.devices.Object)
                .CompleteOrTimeout().Result;

            // Assert
            Assert.False(result);
            this.devices.Verify(x => x.CreateListUsingJobsAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItChangesTheSimulationStatusWhenTheDeviceCreationIsComplete()
        {
            // Arrange
            var simulationId = Guid.NewGuid().ToString();
            var sim = new SimulationModel
            {
                Id = simulationId, Enabled = true, DevicesCreationStarted = false,
                DeviceModels = new List<SimulationModel.DeviceModelRef>
                {
                    new SimulationModel.DeviceModelRef { Id = "some", Count = 3 }
                }
            };
            var record = new ValueApiModel
            {
                Key = simulationId, Data = JsonConvert.SerializeObject(sim)
            };
            this.storage.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(record);

            // Arrange - needed for the record update
            this.storage.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel { Key = simulationId, Data = "{}", ETag = "someETag" });

            // Act
            var result = this.target.TryToSetDeviceCreationCompleteAsync(simulationId)
                .CompleteOrTimeout().Result;

            // Assert
            Assert.True(result);
            this.storage.Verify(
                x => x.UpdateAsync(STORAGE_COLLECTION, simulationId, It.Is<string>(
                    s => JsonConvert.DeserializeObject<SimulationModel>(s).DevicesCreationComplete
                ), It.IsAny<string>()), Times.Once());
        }

        private void ThereAreSomeDeviceModels()
        {
            this.deviceModels.Setup(x => x.GetListAsync())
                .ReturnsAsync(this.models);
        }

        private void ThereAreNoSimulationsInTheStorage()
        {
            this.storage.Setup(x => x.GetAllAsync(STORAGE_COLLECTION)).ReturnsAsync(new ValueListApiModel());
            // In case the test inserts a record, return a valid storage object
            this.storage.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel { Key = SIMULATION_ID, Data = "{}", ETag = "someETag" });
        }
    }
}
