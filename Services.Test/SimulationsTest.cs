// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Statistics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.CosmosDbSql;
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
        private const string SIMULATION_ID = "1";

        private readonly Simulations target;
        private readonly List<DeviceModel> models;

        private readonly Mock<IServicesConfig> mockConfig;
        private readonly Mock<IDeviceModels> deviceModels;
        private readonly Mock<IEngines> enginesFactory;
        private readonly Mock<IStorageAdapterClient> mockStorageAdapterClient;
        private readonly Mock<IEngine> simulationsStorage;
        private readonly Mock<IDevices> devices;
        private readonly Mock<IFileSystem> file;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IDiagnosticsLogger> diagnosticsLogger;
        private readonly Mock<IConnectionStrings> connectionStrings;
        private readonly Mock<ISDKWrapper> mockCosmosDbSqlWrapper;
        private readonly Mock<IDocumentClient> mockDocumentClient;
        private readonly Mock<IResourceResponse<Document>> mockStorageDocument;
        private readonly Mock<ISimulationStatistics> simulationStatistics;

        public SimulationsTest(ITestOutputHelper log)
        {
            this.mockConfig = new Mock<IServicesConfig>();
            this.deviceModels = new Mock<IDeviceModels>();
            this.simulationsStorage = new Mock<IEngine>();
            this.simulationsStorage.Setup(x => x.Init(It.IsAny<Config>())).Returns(this.simulationsStorage.Object);
            this.simulationsStorage.Setup(x => x.BuildRecord(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string id, string json) => new DataRecord { Id = id, Data = json });

            // Multiple tests require the passed-in StorageRecord object to be returned from storage
            this.simulationsStorage.Setup(
                    x => x.UpsertAsync(
                        It.IsAny<IDataRecord>(),
                        It.IsAny<string>()))
                .ReturnsAsync((DataRecord storageRecord, string eTag) => storageRecord);
            this.mockCosmosDbSqlWrapper = new Mock<ISDKWrapper>();
            this.mockDocumentClient = new Mock<IDocumentClient>();
            this.mockStorageDocument = new Mock<IResourceResponse<Document>>();
            this.mockStorageAdapterClient = new Mock<IStorageAdapterClient>();
            this.simulationStatistics = new Mock<ISimulationStatistics>();

            this.enginesFactory = new Mock<IEngines>();
            this.enginesFactory.Setup(x => x.Build(It.IsAny<Config>())).Returns(this.simulationsStorage.Object);

            this.logger = new Mock<ILogger>();
            this.diagnosticsLogger = new Mock<IDiagnosticsLogger>();
            this.devices = new Mock<IDevices>();
            this.file = new Mock<IFileSystem>();
            this.connectionStrings = new Mock<IConnectionStrings>();
            this.models = new List<DeviceModel>
            {
                new DeviceModel { Id = "01" },
                new DeviceModel { Id = "05" },
                new DeviceModel { Id = "02" },
                new DeviceModel { Id = "AA" }
            };

            this.target = new Simulations(
                this.mockConfig.Object,
                this.deviceModels.Object,
                this.enginesFactory.Object,
                this.mockStorageAdapterClient.Object,
                this.connectionStrings.Object,
                this.file.Object,
                this.logger.Object,
                this.diagnosticsLogger.Object,
                this.simulationStatistics.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void InitialListIsEmpty()
        {
            // Arrange
            this.ThereAreNoSimulationsInTheStorage();

            // Act
            var list = this.target.GetListAsync().CompleteOrTimeout().Result;

            // Assert
            Assert.Equal(0, list.Count);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void InitialMetadataAfterCreation()
        {
            // Arrange
            this.ThereAreNoSimulationsInTheStorage();
            this.ThereAreSomeDeviceModels();
            this.StorageReturnsSimulationRecordOnCreate();

            // Act
            SimulationModel result = this.target.InsertAsync(new SimulationModel(), "default")
                .CompleteOrTimeout().Result;

            // Assert
            Assert.False(result.PartitioningComplete);
            Assert.True(Math.Abs(result.Modified.ToUnixTimeSeconds() - result.Created.ToUnixTimeSeconds()) < 10);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreateDefaultSimulation()
        {
            // Arrange
            const int DEFAULT_DEVICE_COUNT = 1;
            this.ThereAreNoSimulationsInTheStorage();
            this.ThereAreSomeDeviceModels();
            var simulation = new SimulationModel();
            this.StorageReturnsSimulationRecordOnCreate(simulation);

            // Return the StorageRecord object that will be passed to StorageRecords
            this.simulationsStorage.Setup(
                    x => x.UpsertAsync(
                        It.IsAny<IDataRecord>(),
                        It.IsAny<string>()))
                .ReturnsAsync((IDataRecord storageRecord, string eTag) => storageRecord);

            // Act
            SimulationModel result = this.target.InsertAsync(simulation, "default")
                .CompleteOrTimeout().Result;

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
            this.ThereAreSomeDeviceModels();
            this.ThereAreNoSimulationsInTheStorage();
            this.StorageReturnsSimulationRecordOnCreate();

            // Act
            SimulationModel result = this.target.InsertAsync(new SimulationModel(), "default")
                .CompleteOrTimeout().Result;

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
            var simulation = new SimulationModel { Id = "this-is-ignored" };
            this.StorageReturnsSimulationRecordOnCreate(simulation);
            SimulationModel result = this.target.InsertAsync(simulation, "default")
                .CompleteOrTimeout().Result;

            // Assert
            Assert.Equal(simulation.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreateWithInvalidTemplate()
        {
            // Act + Assert
            Assert.ThrowsAsync<InvalidInputException>(
                    async () => await this.target.InsertAsync(new SimulationModel(), "mytemplate"))
                .CompleteOrTimeout();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItInvokesGetSimulationStatisticsOnceOnGetWithStatistics()
        {
            // Arrange
            const string ID = "1";

            this.simulationsStorage.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(new DataRecord { Id = ID, Data = "{}" });

            // Act
            this.target.GetWithStatisticsAsync(ID).CompleteOrTimeout();

            // Assert
            this.simulationStatistics
                .Verify(x => x.GetSimulationStatisticsAsync(
                    ID
                ), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreatedSimulationsAreStored()
        {
            // Arrange
            this.ThereAreSomeDeviceModels();
            this.ThereAreNoSimulationsInTheStorage();
            var simulation = new SimulationModel { Id = Guid.NewGuid().ToString(), Enabled = false };
            this.StorageReturnsSimulationRecordOnCreate(simulation);

            // Act
            this.target.InsertAsync(simulation, "default").CompleteOrTimeout();

            // Assert
            this.simulationsStorage.Verify(
                x => x.UpsertAsync(It.IsAny<IDataRecord>(), It.IsAny<string>()), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void UpsertRequiresIdWhileInsertDoesNot()
        {
            // Arrange
            var s1 = new SimulationModel { Name = "Test Simulation 1" };
            var s2 = new SimulationModel { Name = "Test Simulation 2" };
            this.ThereAreNoSimulationsInTheStorage();
            this.StorageReturnsSimulationRecordOnCreate(s1);
            var s2StorageRecord = new DataRecord
            {
                Id = s2.Id,
                Data = JsonConvert.SerializeObject(s2),
            };

            this.simulationsStorage.Setup(x => x.UpsertAsync(It.IsAny<IDataRecord>()))
                .ReturnsAsync(s2StorageRecord);

            // Act - No exception occurs
            this.target.InsertAsync(s1).CompleteOrTimeout();

            // Act + Assert
            Assert.ThrowsAsync<InvalidInputException>(async () => await this.target.UpsertAsync(s2, false))
                .CompleteOrTimeout();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItTestsConnectionStringsWhenCreatingASimulation()
        {
            // Arrange
            this.ThereAreSomeDeviceModels();
            this.ThereAreNoSimulationsInTheStorage();

            // Act
            var simulation = new SimulationModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test",
                IotHubConnectionStrings = new List<string> { "c1", "c2" }
            };
            this.StorageReturnsSimulationRecordOnCreate(simulation);
            SimulationModel result = this.target.InsertAsync(simulation)
                .CompleteOrTimeout().Result;

            // Assert
            this.connectionStrings.Verify(x => x.SaveAsync("c1", true), Times.Once);
            this.connectionStrings.Verify(x => x.SaveAsync("c2", true), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void UpsertFailsWhenETagsDoNotMatch()
        {
            // Arrange
            const string ETAG1 = "ETag 001";
            const string ETAG2 = "ETag 002";

            // Mock simulation that will be returned from storage
            // Create a mock Cosmos DB SQL Document object that will contain a
            // different ETag than the one we're trying to use to upsert
            var updatedSimulation = new SimulationModel
            {
                Id = SIMULATION_ID,
                Name = "Test Simulation 2",
                ETag = ETAG2
            };
            var record = new DataRecord { Id = "foo" };
            record.SetETag(ETAG2);
            record.SetData(JsonConvert.SerializeObject(updatedSimulation));
            this.simulationsStorage.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(record);

            // Act + Assert
            var initialSimulation = new SimulationModel
            {
                Id = SIMULATION_ID,
                Name = "Test Simulation 1",
                ETag = ETAG1
            };
            Assert.ThrowsAsync<ResourceOutOfDateException>(
                    async () => await this.target.UpsertAsync(initialSimulation, false))
                .CompleteOrTimeout();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void UpsertSucceedsWhenETagsMatch()
        {
            // Arrange
            const string ETAG1 = "ETag 001";
            const string ETAG2 = "ETag 002";

            // Record in storage
            var existingSimulation = new SimulationModel { Id = SIMULATION_ID };
            var existingRecord = new DataRecord
            {
                Id = SIMULATION_ID,
                Data = JsonConvert.SerializeObject(existingSimulation)
            };
            existingRecord.SetETag(ETAG1);

            // Record to write
            var newSimulation = new SimulationModel
            {
                Id = SIMULATION_ID,
                ETag = existingRecord.GetETag()
            };

            // Record after writing to storage
            var updatedRecord = new DataRecord { Id = SIMULATION_ID };
            updatedRecord.SetETag(ETAG2);

            this.simulationsStorage.Setup(x => x.GetAsync(SIMULATION_ID))
                .ReturnsAsync(existingRecord);
            this.simulationsStorage.Setup(x => x.UpsertAsync(It.IsAny<IDataRecord>(), It.IsAny<string>()))
                .ReturnsAsync(updatedRecord);

            // Act
            var result = this.target.UpsertAsync(newSimulation, false)
                .CompleteOrTimeout().Result;

            // Assert
            Assert.Matches(ETAG2, result.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesNewSimulationsWithPartitioningStateNotComplete()
        {
            // Arrange
            this.simulationsStorage.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync((DataRecord) null);
            var sim = new SimulationModel
            {
                Id = "1",
                Enabled = true,
                PartitioningComplete = true
            };

            IDataRecord record = new DataRecord();
            record.SetData(JsonConvert.SerializeObject(sim));

            this.simulationsStorage.Setup(x => x.UpsertAsync(It.IsAny<IDataRecord>()))
                .ReturnsAsync(record);

            // Act
            SimulationModel result = this.target.UpsertAsync(sim, false)
                .CompleteOrTimeout().Result;

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
                PartitioningComplete = true,
                ETag = "*"
            };

            IDataRecord storageRecord = new DataRecord();
            storageRecord.SetData(JsonConvert.SerializeObject(sim)).SetETag("*");

            this.simulationsStorage.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(storageRecord);
            this.simulationsStorage.Setup(x => x.UpsertAsync(It.IsAny<IDataRecord>(), It.IsAny<string>()))
                .ReturnsAsync(storageRecord);

            // Act
            var update = new SimulationModel
            {
                Id = sim.Id,
                Enabled = false,
                PartitioningComplete = true,
                ETag = "*"
            };
            var result = this.target.UpsertAsync(update, false).CompleteOrTimeout().Result;

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
            var eTagValue = "*";
            var sim = new SimulationModel
            {
                Id = "1",
                Enabled = true,
                PartitioningComplete = true,
                ETag = eTagValue
            };

            IDataRecord storageRecord = new DataRecord { Id = sim.Id };
            storageRecord
                .SetData(JsonConvert.SerializeObject(sim))
                .SetETag(eTagValue);

            this.simulationsStorage.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(storageRecord);

            // Act
            var result = this.target.TryToStartDevicesCreationAsync(sim.Id, this.devices.Object)
                .CompleteOrTimeout().Result;

            // Assert
            Assert.True(result);
            this.devices.Verify(x => x.CreateListUsingJobsAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            this.simulationsStorage.Verify(
                x => x.UpsertAsync(
                    It.Is<IDataRecord>(
                        sr => JsonConvert.DeserializeObject<SimulationModel>(sr.GetData()).DevicesCreationStarted),
                    It.IsAny<string>()), Times.Once());
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

            IDataRecord storageRecord = new DataRecord { Id = "foo" };
            storageRecord.SetData(JsonConvert.SerializeObject(sim)).SetETag("*");

            this.simulationsStorage.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(storageRecord);

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

            IDataRecord storageRecord = new DataRecord { Id = "foo" };
            storageRecord.SetData(JsonConvert.SerializeObject(sim)).SetETag("*");

            this.simulationsStorage.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(storageRecord);
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
                Id = simulationId,
                Enabled = true,
                DevicesCreationStarted = false,
                DeviceModels = new List<SimulationModel.DeviceModelRef>
                {
                    new SimulationModel.DeviceModelRef { Id = "some", Count = 3 }
                }
            };

            IDataRecord storageRecord = new DataRecord { Id = simulationId };
            storageRecord.SetData(JsonConvert.SerializeObject(sim)).SetETag("*");

            this.simulationsStorage.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(storageRecord);

            // Act
            var result = this.target.TryToSetDeviceCreationCompleteAsync(simulationId)
                .CompleteOrTimeout().Result;

            // Assert - operation succeeded
            Assert.True(result);

            // Assert - DevicesCreationComplete is set to true
            Func<string, SimulationModel> deserialize = JsonConvert.DeserializeObject<SimulationModel>;
            this.simulationsStorage.Verify(x => x.BuildRecord(sim.Id, It.Is<string>(
                sr => deserialize(sr).DevicesCreationComplete == true)), Times.Once());

            // Assert - DevicesDeletionComplete, DeviceDeletionJobId and DevicesDeletionStarted are reset
            this.simulationsStorage.Verify(x => x.BuildRecord(sim.Id, It.Is<string>(
                sr => deserialize(sr).DevicesDeletionComplete == false)), Times.Once());
            this.simulationsStorage.Verify(x => x.BuildRecord(sim.Id, It.Is<string>(
                sr => deserialize(sr).DeviceDeletionJobId == null)), Times.Once());
            this.simulationsStorage.Verify(x => x.BuildRecord(sim.Id, It.Is<string>(
                sr => deserialize(sr).DevicesDeletionStarted == false)), Times.Once());

            // Assert - only one write
            this.simulationsStorage.Verify(x => x.UpsertAsync(It.IsAny<IDataRecord>(), It.IsAny<string>()), Times.Once());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItStartsTheDeviceDeletionOnlyIfNotStarted()
        {
            // Arrange
            var simulationId = Guid.NewGuid().ToString();
            var sim = new SimulationModel
            {
                Id = simulationId,
                Enabled = true,
                DevicesDeletionStarted = true,
                DeviceModels = new List<SimulationModel.DeviceModelRef>
                {
                    new SimulationModel.DeviceModelRef { Id = "some", Count = 3 }
                }
            };

            IDataRecord storageRecord = new DataRecord { Id = "foo" };
            storageRecord.SetData(JsonConvert.SerializeObject(sim)).SetETag("*");

            this.simulationsStorage.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(storageRecord);

            // Act
            var result = this.target.TryToStartDevicesDeletionAsync(simulationId, this.devices.Object)
                .CompleteOrTimeout().Result;

            // Assert
            Assert.True(result);
            this.devices.Verify(x => x.DeleteListUsingJobsAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItStartsTheDeviceBulkDeletionUsingJobs()
        {
            // Arrange
            var eTagValue = "*";
            var sim = new SimulationModel
            {
                Id = "1",
                Enabled = true,
                PartitioningComplete = true,
                ETag = eTagValue
            };

            IDataRecord storageRecord = new DataRecord { Id = sim.Id };
            storageRecord.SetData(JsonConvert.SerializeObject(sim)).SetETag(eTagValue);

            this.simulationsStorage.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(storageRecord);

            // Act
            var result = this.target.TryToStartDevicesDeletionAsync(sim.Id, this.devices.Object)
                .CompleteOrTimeout().Result;

            // Assert
            Assert.True(result);
            this.devices.Verify(x => x.DeleteListUsingJobsAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            this.simulationsStorage.Verify(
                x => x.UpsertAsync(
                    It.Is<IDataRecord>(
                        sr => JsonConvert.DeserializeObject<SimulationModel>(sr.GetData()).DevicesDeletionStarted),
                    It.IsAny<string>()), Times.Once());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReportsIfTheDeviceDeletionStartFails()
        {
            // Arrange
            var simulationId = Guid.NewGuid().ToString();
            var sim = new SimulationModel
            {
                Id = simulationId,
                Enabled = true,
                DevicesCreationStarted = false,
                DeviceModels = new List<SimulationModel.DeviceModelRef>
                {
                    new SimulationModel.DeviceModelRef { Id = "some", Count = 3 }
                }
            };

            IDataRecord storageRecord = new DataRecord { Id = "foo" };
            storageRecord.SetData(JsonConvert.SerializeObject(sim)).SetETag("*");

            this.simulationsStorage.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(storageRecord);
            this.devices.Setup(x => x.DeleteListUsingJobsAsync(It.IsAny<IEnumerable<string>>()))
                .Throws<SomeException>();

            // Act
            var result = this.target.TryToStartDevicesDeletionAsync(simulationId, this.devices.Object)
                .CompleteOrTimeout().Result;

            // Assert
            Assert.False(result);
            this.devices.Verify(x => x.DeleteListUsingJobsAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItTryToCreateDefaultSimulationsWhenThereIsNoDefaultSimulationsInStorage()
        {
            // Arrange
            this.ThereIsNoDefaultSimulationsInStorage();
            this.ThereIsATemplateForDefaultSimulations();

            // Act
            this.target.TrySeedAsync().CompleteOrTimeout();

            // Assert
            this.simulationsStorage.Verify(x => x.CreateAsync(It.IsAny<IDataRecord>()), Times.Once);
        }

        private void ThereIsATemplateForDefaultSimulations()
        {
            var simulationList = new List<SimulationModel>()
            {
                new SimulationModel()
            };
            string fileContent = JsonConvert.SerializeObject(simulationList);
            const string TEMPLATE_FILE_PATH = "/data/";
            this.mockConfig.Setup(x => x.SeedTemplateFolder).Returns(TEMPLATE_FILE_PATH);
            this.mockConfig.Setup(x => x.SeedTemplate).Returns("template");
            this.file.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            this.file.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(fileContent);
        }

        private void ThereIsNoDefaultSimulationsInStorage()
        {
            this.simulationsStorage.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            this.simulationsStorage.Setup(x => x.GetAsync(It.IsAny<string>())).ThrowsAsync(new ResourceNotFoundException());
        }

        private void ThereAreSomeDeviceModels()
        {
            this.deviceModels.Setup(x => x.GetListAsync())
                .ReturnsAsync(this.models);
        }

        private void ThereAreNoSimulationsInTheStorage()
        {
            this.simulationsStorage.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<IDataRecord>());

            // In case the test inserts a record, return a valid StorageRecord object
            IDataRecord record = new DataRecord { Id = SIMULATION_ID, Data = "{}" };
            this.simulationsStorage.Setup(x => x.UpsertAsync(It.IsAny<IDataRecord>()))
                .ReturnsAsync(record);
        }

        private void StorageReturnsSimulationRecordOnCreate(SimulationModel simulation = null)
        {
            if (simulation == null)
            {
                simulation = new SimulationModel { Id = Guid.NewGuid().ToString(), Enabled = false };
            }

            IDataRecord simulationRecord = new DataRecord
            {
                Id = simulation.Id,
                Data = JsonConvert.SerializeObject(simulation),
            };
            this.simulationsStorage.Setup(x => x.CreateAsync(It.IsAny<IDataRecord>()))
                .ReturnsAsync(simulationRecord);
        }
    }
}
