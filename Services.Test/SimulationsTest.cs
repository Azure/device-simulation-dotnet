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
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.DocumentDb;
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

        private const string SIMULATION_ID = "1";

        private readonly Mock<IServicesConfig> mockConfig;
        private readonly Mock<IDeviceModels> deviceModels;
        private readonly Mock<IFactory> mockFactory;
        private readonly Mock<IStorageAdapterClient> mockStorageAdapterClient;
        private readonly Mock<IStorageRecords> mockStorageRecords;
        private readonly Mock<IDevices> devices;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IDiagnosticsLogger> diagnosticsLogger;
        private readonly Mock<IIotHubConnectionStringManager> connStringManager;
        private readonly Mock<IDocumentDbWrapper> mockDocumentDbWrapper;
        private readonly Mock<IDocumentClient> mockDocumentClient;
        private readonly Mock<IResourceResponse<Document>> mockStorageDocument;
        private readonly Simulations target;
        private readonly List<DeviceModel> models;

        public SimulationsTest(ITestOutputHelper log)
        {
            this.log = log;

            this.mockConfig = new Mock<IServicesConfig>();
            this.deviceModels = new Mock<IDeviceModels>();
            this.mockStorageRecords = new Mock<IStorageRecords>();
            this.mockStorageRecords.Setup(x => x.Init(It.IsAny<StorageConfig>())).Returns(this.mockStorageRecords.Object);

            // Multiple tests require the passed-in StorageRecord object to be returned from storage
            this.mockStorageRecords.Setup(
                    x => x.UpsertAsync(
                        It.IsAny<StorageRecord>(),
                        It.IsAny<string>()))
                .ReturnsAsync((StorageRecord storageRecord, string eTag) => storageRecord);
            this.mockDocumentDbWrapper = new Mock<IDocumentDbWrapper>();
            this.mockDocumentClient = new Mock<IDocumentClient>();
            this.mockStorageDocument = new Mock<IResourceResponse<Document>>();
            this.mockStorageAdapterClient = new Mock<IStorageAdapterClient>();

            this.mockFactory = new Mock<IFactory>();
            this.mockFactory.Setup(x => x.Resolve<IStorageRecords>()).Returns(this.mockStorageRecords.Object);

            this.logger = new Mock<ILogger>();
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
                this.mockConfig.Object,
                this.deviceModels.Object,
                this.mockFactory.Object,
                this.mockStorageAdapterClient.Object,
                this.connStringManager.Object,
                this.devices.Object,
                this.logger.Object,
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
            this.StorageReturnsSimulationRecordOnCreate();

            // Act
            SimulationModel result = this.target.InsertAsync(new SimulationModel(), "default").Result;

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
            this.mockStorageRecords.Setup(
                    x => x.UpsertAsync(
                        It.IsAny<StorageRecord>(),
                        It.IsAny<string>()))
                .ReturnsAsync((StorageRecord storageRecord, string eTag) => storageRecord);

            // Act
            SimulationModel result = this.target.InsertAsync(simulation, "default").Result;

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
            this.StorageReturnsSimulationRecordOnCreate();

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
            this.StorageReturnsSimulationRecordOnCreate(simulation);
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
            var simulation = new SimulationModel { Id = Guid.NewGuid().ToString(), Enabled = false };
            this.StorageReturnsSimulationRecordOnCreate(simulation);

            // Act
            this.target.InsertAsync(simulation, "default").Wait();

            // Assert
            this.mockStorageRecords.Verify(
                x => x.UpsertAsync(It.IsAny<StorageRecord>(), It.IsAny<string>()), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void UpsertRequiresIdWhileInsertDoesNot()
        {
            // Arrange
            var s1 = new SimulationModel() { Name = "Test Simulation 1" };
            var s2 = new SimulationModel() { Name = "Test Simulation 2" };
            this.ThereAreNoSimulationsInTheStorage();
            this.StorageReturnsSimulationRecordOnCreate(s1);
            var s2StorageRecord = new StorageRecord
            {
                Id = s2.Id,
                Data = JsonConvert.SerializeObject(s2),
            };

            this.mockStorageRecords.Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>()))
                .ReturnsAsync(s2StorageRecord);

            // Act - No exception occurs
            this.target.InsertAsync(s1).Wait(Constants.TEST_TIMEOUT);

            // Act + Assert
            Assert.ThrowsAsync<InvalidInputException>(async () => await this.target.UpsertAsync(s2))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void UpsertWillFailWhenETagsDoNotMatch()
        {
            // Arrange
            const string ETAG1 = "ETag 001";
            const string ETAG2 = "ETag 002";

            // Mock simulation that will be returned from storage
            var updatedSimulation = new SimulationModel { Id = SIMULATION_ID, Name = "Test Simulation 2", ETag = ETAG2 };
            var updatedStorageRecord = new StorageRecord
            {
                Id = SIMULATION_ID,
                Data = JsonConvert.SerializeObject(updatedSimulation),
            };

            // Create a mock DocumentDB Document object that will contain a
            // different ETag than the one we're trying to use to upsert
            var document = new Document();
            document.Id = "foo";
            document.SetPropertyValue("_etag", ETAG2);
            document.SetPropertyValue("Data", JsonConvert.SerializeObject(updatedSimulation));
            var mockStorageRecord = StorageRecord.FromDocumentDb(document);
            this.mockStorageRecords.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(mockStorageRecord);

            // Initial simulation 
            var initialSimulation = new SimulationModel { Id = SIMULATION_ID, Name = "Test Simulation 1", ETag = ETAG1 };
            var initialStorageRecord = new StorageRecord
            {
                Id = SIMULATION_ID,
                Data = JsonConvert.SerializeObject(initialSimulation),
            };

            // Act + Assert
            Assert.ThrowsAsync<ResourceOutOfDateException>(
                    async () => await this.target.UpsertAsync(initialSimulation))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact]
        public void UpsertWillSucceedWhenETagsMatch()
        {
            // Arrange
            const string ETAG1 = "ETag 001";
            const string ETAG2 = "ETag 002";

            // Mock simulation that will be returned from storage
            var existingSimulation = new SimulationModel { Id = SIMULATION_ID, Name = "Test Simulation 2", ETag = ETAG1 };
            var updatedStorageRecord = new StorageRecord
            {
                Id = SIMULATION_ID,
                Data = JsonConvert.SerializeObject(existingSimulation),
            };

            // Create a mock DocumentDB Document object that will contain the
            // same ETag value as the one we're trying to use to upsert with.
            var document = new Document();
            document.Id = "foo";
            document.SetPropertyValue("_etag", ETAG1);
            document.SetPropertyValue("Data", JsonConvert.SerializeObject(existingSimulation));
            var mockStorageRecord = StorageRecord.FromDocumentDb(document);

            // Initial simulation 
            var initialSimulation = new SimulationModel { Id = SIMULATION_ID, Name = "Test Simulation 1", ETag = ETAG1 };
            var initialStorageRecord = new StorageRecord
            {
                Id = SIMULATION_ID,
                Data = JsonConvert.SerializeObject(initialSimulation),
            };

            // Create a second document that will be returned after the upsert,
            // which will contain an updated ETag
            var upsertResultDocument = new Document();
            upsertResultDocument.Id = "bar";
            upsertResultDocument.SetPropertyValue("_etag", ETAG2);
            upsertResultDocument.SetPropertyValue("Data", JsonConvert.SerializeObject(initialSimulation));
            var upsertResultStorageRecord = StorageRecord.FromDocumentDb(upsertResultDocument);

            this.mockStorageRecords.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(mockStorageRecord);
            this.mockStorageRecords.Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>(), It.IsAny<string>())).ReturnsAsync(upsertResultStorageRecord);

            // Act
            var returnedSimulationTask = this.target.UpsertAsync(initialSimulation);
            returnedSimulationTask.Wait(Constants.TEST_TIMEOUT);

            // Assert
            Assert.Matches(ETAG2, returnedSimulationTask.Result.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesNewSimulationsWithPartitioningStateNotComplete()
        {
            // Arrange
            this.mockStorageRecords.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync((StorageRecord)null);
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

            // Create a DocumentDB Document that will be used to create a StorageRecord object
            var document = new Document();
            document.Id = "foo";
            document.SetPropertyValue("Data", JsonConvert.SerializeObject(sim));
            var storageRecord = StorageRecord.FromDocumentDb(document);

            this.mockStorageRecords.Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>()))
                .ReturnsAsync(storageRecord);

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
                PartitioningComplete = true,
                ETag = "*"
            };
            var record = new ValueApiModel
            {
                Key = "1",
                Data = JsonConvert.SerializeObject(sim)
            };

            // Create a DocumentDB Document that will be used to create a StorageRecord object
            var document = new Document();
            document.Id = "foo";
            document.SetPropertyValue("Data", JsonConvert.SerializeObject(sim));
            document.SetPropertyValue("ETag", "*");
            var storageRecord = StorageRecord.FromDocumentDb(document);

            this.mockStorageRecords.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(storageRecord);
            this.mockStorageRecords.Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>(), It.IsAny<string>()))
                .ReturnsAsync(storageRecord);

            // Act
            var update = new SimulationModel
            {
                Id = sim.Id,
                Enabled = false,
                PartitioningComplete = true,
                ETag = "*"
            };
            var result = this.target.UpsertAsync(update).CompleteOrTimeout().Result;

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

            // Create a DocumentDB Document that will be used to create a StorageRecord object
            var document = new Document();
            document.Id = "foo";
            document.SetPropertyValue("Data", JsonConvert.SerializeObject(sim));
            document.SetPropertyValue("ETag", eTagValue);
            var storageRecord = StorageRecord.FromDocumentDb(document);

            this.mockStorageRecords.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(storageRecord);

            // Act
            var result = this.target.TryToStartDevicesCreationAsync(sim.Id, this.devices.Object)
                .CompleteOrTimeout().Result;

            // Assert
            Assert.True(result);
            this.devices.Verify(x => x.CreateListUsingJobsAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            this.mockStorageRecords.Verify(
                x => x.UpsertAsync(
                    It.Is<StorageRecord>(
                        sr => JsonConvert.DeserializeObject<SimulationModel>(sr.Data).DevicesCreationStarted),
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

            // Create a DocumentDB Document that will be used to create a StorageRecord object
            var document = new Document();
            document.Id = "foo";
            document.SetPropertyValue("Data", JsonConvert.SerializeObject(sim));
            document.SetPropertyValue("ETag", "*");
            var storageRecord = StorageRecord.FromDocumentDb(document);

            this.mockStorageRecords.Setup(x => x.GetAsync(It.IsAny<string>()))
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

            // Create a DocumentDB Document that will be used to create a StorageRecord object
            var document = new Document();
            document.Id = "foo";
            document.SetPropertyValue("Data", JsonConvert.SerializeObject(sim));
            document.SetPropertyValue("ETag", "*");
            var storageRecord = StorageRecord.FromDocumentDb(document);

            this.mockStorageRecords.Setup(x => x.GetAsync(It.IsAny<string>()))
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

            // Create a DocumentDB Document that will be used to create a StorageRecord object
            var document = new Document();
            document.Id = "foo";
            document.SetPropertyValue("Data", JsonConvert.SerializeObject(sim));
            document.SetPropertyValue("ETag", "*");
            var storageRecord = StorageRecord.FromDocumentDb(document);

            this.mockStorageRecords.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(storageRecord);

            // Act
            var result = this.target.TryToSetDeviceCreationCompleteAsync(simulationId)
                .CompleteOrTimeout().Result;

            // Assert
            Assert.True(result);
            this.mockStorageRecords.Verify(
                x => x.UpsertAsync(
                    It.Is<StorageRecord>(
                        sr => JsonConvert.DeserializeObject<SimulationModel>(sr.Data).DevicesCreationComplete),
                    It.IsAny<string>()), Times.Once());
        }

        private void ThereAreSomeDeviceModels()
        {
            this.deviceModels.Setup(x => x.GetListAsync())
                .ReturnsAsync(this.models);
        }

        private void ThereAreNoSimulationsInTheStorage()
        {
            this.mockStorageRecords.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<StorageRecord>());

            // In case the test inserts a record, return a valid StorageRecord object
            this.mockStorageRecords.Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>()))
                .ReturnsAsync(new StorageRecord() { Id = SIMULATION_ID, Data = "{}" });
        }

        private void StorageReturnsSimulationRecordOnCreate(SimulationModel simulation = null)
        {
            if (simulation == null)
            {
                simulation = new SimulationModel { Id = Guid.NewGuid().ToString(), Enabled = false };
            }

            var simulationRecord = new StorageRecord
            {
                Id = simulation.Id,
                Data = JsonConvert.SerializeObject(simulation),
            };
            this.mockStorageRecords.Setup(x => x.CreateAsync(It.IsAny<StorageRecord>()))
                .ReturnsAsync(simulationRecord);
        }
    }
}
