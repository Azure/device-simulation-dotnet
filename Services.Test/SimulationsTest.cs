// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        private const string STORAGE_COLLECTION = "simulations";
        private const string SIMULATION_ID = "1";

        private readonly Mock<IServicesConfig> mockConfig;
        private readonly Mock<IDeviceModels> deviceModels;
        private readonly Mock<IFactory> mockFactory;
        private readonly Mock<IStorageAdapterClient> mockStorageAdapterClient;
        private readonly Mock<IStorageRecords> mockStorageRecords;
        private readonly Mock<IDevices> devices;
        private readonly Mock<ILogger> logger;
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
            this.mockDocumentDbWrapper = new Mock<IDocumentDbWrapper>();
            this.mockDocumentClient = new Mock<IDocumentClient>();
            this.mockStorageDocument = new Mock<IResourceResponse<Document>>();

            this.mockFactory = new Mock<IFactory>();
            this.mockFactory.Setup(x => x.Resolve<IStorageRecords>()).Returns(this.mockStorageRecords.Object);

            this.mockStorageAdapterClient = new Mock<IStorageAdapterClient>();
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

            this.target = new Simulations(
                this.mockConfig.Object, 
                this.deviceModels.Object, 
                this.mockFactory.Object,
                this.mockStorageAdapterClient.Object, 
                this.connStringManager.Object, 
                this.devices.Object, 
                this.logger.Object);
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
            Assert.Equal(result.Created, result.Modified);
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

            // Act
            SimulationModel result = this.target.InsertAsync(simulation, "default").Result;

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
            //this.mockStorageAdapterClient.Verify(
            //    x => x.UpdateAsync(STORAGE_COLLECTION, SIMULATION_ID, It.IsAny<string>(), "*"));
            this.mockStorageRecords.Verify(
                x => x.CreateAsync(It.IsAny<StorageRecord>()), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void SimulationsCanBeUpserted()
        {
            // Arrange
            this.ThereAreSomeDeviceModels();
            this.ThereAreNoSimulationsInTheStorage();
            var simulation = new SimulationModel
            {
                Id = SIMULATION_ID,
                Enabled = false,
                ETag = "oldETag"
            };
            var updatedSimulation = new SimulationModel
            {
                Id = SIMULATION_ID,
                Enabled = false,
                ETag = "newETag"
            };
            var updatedStorageRecord = new StorageRecord
            {
                Id = updatedSimulation.Id,
                Data = JsonConvert.SerializeObject(updatedSimulation)
            };
            this.mockStorageRecords.Setup(
                    x => x.GetAsync(It.IsAny<string>())
                )
                .ReturnsAsync(updatedStorageRecord);

            // Act
            var upsertTask = this.target.UpsertAsync(simulation);
            upsertTask.Wait(Constants.TEST_TIMEOUT);
            simulation = upsertTask.Result;

            // Assert
            this.mockStorageRecords.Verify(
                x => x.UpsertAsync(It.IsAny<StorageRecord>())
            );
            Assert.Equal("newETag", simulation.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void UpsertRequiresIdWhileInsertDoesNot()
        {
            // Arrange
            var s1 = new SimulationModel() { Name = "Test Simulation 1"};
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
        public void UpsertUsesOptimisticConcurrency()
        {
            // Arrange
            const string ETAG1 = "001";
            const string ETAG2 = "002";

            //// Set up a DocumentDbWrapper Mock to return the mock storage document. This is
            //// necessary because the ETag property of StorageDocument, which we're using for
            //// this test, is read only.
            //var document = new Document();
            //document.Id = SIMULATION_ID;
            //document.re
            //this.mockStorageDocument.SetupGet(x => x.Resource).Returns(document);

            //this.mockDocumentDbWrapper.Setup(
            //    x => x.GetClientAsync(
            //        It.IsAny<StorageConfig>())
            //).ReturnsAsync(this.mockDocumentClient.Object);

            //this.mockDocumentDbWrapper.Setup(
            //    x => x.ReadAsync(
            //        It.IsAny<IDocumentClient>(),
            //        It.IsAny<StorageConfig>(),
            //        It.IsAny<string>())
            //).ReturnsAsync(this.mockStorageDocument.Object);

            // Initial simulation 
            var initialSimulation = new SimulationModel { Id = SIMULATION_ID, Name = "Test Simulation 1", ETag = ETAG1 };
            var initialStorageRecord = new StorageRecord
            {
                Id = SIMULATION_ID,
                Data = JsonConvert.SerializeObject(initialSimulation),
            };

            // Simulation after update
            var updatedSimulation = new SimulationModel { Id = SIMULATION_ID, Name = "Test Simulation 2", ETag = ETAG2 };
            var updatedStorageRecord = new StorageRecord
            {
                Id = SIMULATION_ID,
                Data = JsonConvert.SerializeObject(updatedSimulation),
            };

            // Initial setup - the ETag matches
            this.mockStorageRecords.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(initialStorageRecord);
            this.mockStorageRecords.Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>()))
                .ReturnsAsync(initialStorageRecord);

            // Act - No exception because ETag matches
            // Note: the call to UpsertAsync modifies the object, don't reuse the variable later
            this.target.UpsertAsync(initialSimulation).Wait(Constants.TEST_TIMEOUT);

            // Arrange - the ETag won't match
            this.mockStorageRecords.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(updatedStorageRecord);

            // Act + Assert
            var outOfDateSimulation = new SimulationModel { Id = SIMULATION_ID, ETag = ETAG1 };

            Assert.ThrowsAsync<ResourceOutOfDateException>(
                    async () => await this.target.UpsertAsync(outOfDateSimulation))
                .Wait(Constants.TEST_TIMEOUT);
        }

        // TODO: this test doesn't validate properties of a device model; it doesn't appear to test what its name implies that it does
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ThereAreNoNullPropertiesInTheDeviceModel()
        {
            // Arrange
            this.ThereAreSomeDeviceModels();
            this.ThereAreNoSimulationsInTheStorage();

            // Arrange the simulation data returned by the mockStorageAdapterClient adapter
            var id = SIMULATION_ID;
            var simulation = new SimulationModel
            {
                Id = id,
                Name = "Test Simulation",
                ETag = "ETag0",
                Enabled = true
            };
            //var updatedValue = new ValueApiModel
            //{
            //    Key = id,
            //    Data = JsonConvert.SerializeObject(simulation),
            //    ETag = simulation.ETag
            //};
            var updatedStorageRecord = new StorageRecord
            {
                Id = id,
                Data = JsonConvert.SerializeObject(simulation),
            };

            //this.mockStorageAdapterClient.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            //    .ReturnsAsync(updatedValue);
            this.mockStorageRecords.Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>())).ReturnsAsync(updatedStorageRecord);

            // Act
            this.target.UpsertAsync(simulation).Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.mockStorageRecords.Verify(x => x.UpsertAsync(
                updatedStorageRecord));
        }

        private void ThereAreSomeDeviceModels()
        {
            this.deviceModels.Setup(x => x.GetListAsync())
                .ReturnsAsync(this.models);
        }

        private void ThereAreNoSimulationsInTheStorage()
        {
            this.mockStorageRecords.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<StorageRecord>());
            
            // In case the test inserts a record, return a valid mockStorageAdapterClient object
            //this.mockStorageAdapterClient.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            //    .ReturnsAsync(new ValueApiModel { Key = SIMULATION_ID, Data = "{}", ETag = "someETag" });

            // In case the test inserts a record, return a valid StorageRecord object
            this.mockStorageRecords.Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>()))
                .ReturnsAsync(new StorageRecord() {Id = SIMULATION_ID, Data = "{}"});
        }

        private void ThereIsAnEnabledSimulationInTheStorage()
        {
            var simulation = new SimulationModel
            {
                Id = SIMULATION_ID,
                Created = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                Modified = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                ETag = "ETag0",
                Enabled = true
            };

            var list = new ValueListApiModel();
            var value = new ValueApiModel
            {
                Key = SIMULATION_ID,
                Data = JsonConvert.SerializeObject(simulation),
                ETag = simulation.ETag
            };
            list.Items.Add(value);

            this.mockStorageAdapterClient.Setup(x => x.GetAllAsync(STORAGE_COLLECTION)).ReturnsAsync(list);
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
