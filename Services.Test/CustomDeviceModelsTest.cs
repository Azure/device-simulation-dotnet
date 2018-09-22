// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.DocumentDb;
using Moq;
using Newtonsoft.Json;
using Services.Test.helpers;
using Xunit;

namespace Services.Test
{
    public class CustomDeviceModelsTest
    {
        private const string STORAGE_COLLECTION = "deviceModels";

        private readonly Mock<IServicesConfig> mockConfig;
        private readonly Mock<IFactory> mockFactory;
        private readonly Mock<IStorageRecords> mockStorageRecords;
        private readonly Mock<IDocumentDbWrapper> mockDocumentDbWrapper;
        private readonly Mock<IDocumentClient> mockDocumentClient;
        private readonly Mock<IResourceResponse<Document>> mockStorageDocument;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IDiagnosticsLogger> diagnosticsLogger;
        private readonly CustomDeviceModels target;

        public CustomDeviceModelsTest()
        {
            this.mockConfig = new Mock<IServicesConfig>();
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

            this.mockFactory = new Mock<IFactory>();
            this.mockFactory.Setup(x => x.Resolve<IStorageRecords>()).Returns(this.mockStorageRecords.Object);

            this.logger = new Mock<ILogger>();
            this.diagnosticsLogger = new Mock<IDiagnosticsLogger>();

            this.target = new CustomDeviceModels(
                this.mockConfig.Object,
                this.mockFactory.Object,
                this.logger.Object,
                this.diagnosticsLogger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void InitialListIsEmpty()
        {
            // Arrange
            this.ThereAreNoCustomDeviceModelsInStorage();

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            Assert.Empty(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesDeviceModelsWithProvidedIdAndRemovingEtag()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var eTag = Guid.NewGuid().ToString();
            var deviceModel = new DeviceModel { Id = id, ETag = eTag };

            this.mockStorageRecords
                .Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>(), It.IsAny<string>()))
                .ReturnsAsync(BuildStorageRecordList(deviceModel));

            // Act
            DeviceModel result = this.target.InsertAsync(deviceModel, false).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModel.Id, result.Id);
            Assert.Equal(deviceModel.ETag, result.ETag);
            this.mockStorageRecords.Verify(x => x.UpsertAsync(
                new StorageRecord
                {
                    Id = id,
                    Data = It.Is<string>(json => JsonConvert.DeserializeObject<DeviceModelScript>(json).Id == id && !json.Contains("ETag"))
                },
                "oldEtag"
            ), Times.Once());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CustomDeviceModelsCanBeUpserted()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();

            var oldDeviceModel = new DeviceModel { Id = id, ETag = "oldEtag" };
            this.TheModelExists(id, oldDeviceModel);

            var updatedDeviceModel = new DeviceModel { Id = id, ETag = "newETag" };
            this.mockStorageRecords
                .Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>(), It.IsAny<string>()))
                .ReturnsAsync(BuildStorageRecordList(updatedDeviceModel));

            // Act
            this.target.UpsertAsync(oldDeviceModel)
                .Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.mockStorageRecords.Verify(x => x.GetAsync(id), Times.Once);
            this.mockStorageRecords.Verify(x => x.UpsertAsync(
                new StorageRecord
                {
                    Id = id,
                    Data = It.Is<string>(json => JsonConvert.DeserializeObject<DeviceModelScript>(json).Id == id && !json.Contains("ETag"))
                },
                "oldEtag"
            ), Times.Once());

            Assert.Equal(updatedDeviceModel.Id, oldDeviceModel.Id);
            // The call to UpsertAsync() modifies the object, so the ETags will match
            Assert.Equal(updatedDeviceModel.ETag, oldDeviceModel.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesDeviceModelWhenDeviceModelNotFoundInUpserting()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var deviceModel = new DeviceModel { Id = id, ETag = "Etag" };
            this.TheModelDoesntExist(id);
            this.mockStorageRecords
                .Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>(), It.IsAny<string>()))
                .ReturnsAsync(BuildStorageRecordList(deviceModel));

            // Act
            this.target.UpsertAsync(deviceModel).Wait(TimeSpan.FromSeconds(30));

            // Assert - the app uses PUT with given ID
            this.mockStorageRecords.Verify(x => x.UpsertAsync(
                new StorageRecord { Id = id, Data = It.IsAny<string>() },
                null));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsConflictingResourceExceptionIfEtagDoesNotMatchInUpserting()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var deviceModelInStorage = new DeviceModel { Id = id, ETag = "ETag" };
            this.TheModelExists(id, deviceModelInStorage);

            // Act & Assert
            var deviceModel = new DeviceModel { Id = id, ETag = "not-matching-Etag" };
            Assert.ThrowsAsync<ConflictingResourceException>(
                    async () => await this.target.UpsertAsync(deviceModel))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenInsertDeviceModelFailed()
        {
            // Arrange
            var deviceModel = new DeviceModel { Id = "id", ETag = "Etag" };
            this.mockStorageRecords
                .Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>(), It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.InsertAsync(deviceModel))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItFailsToUpsertWhenUnableToFetchModelFromStorage()
        {
            // Arrange
            var deviceModel = new DeviceModel { Id = "id", ETag = "Etag" };
            this.mockStorageRecords
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.UpsertAsync(deviceModel))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItFailsToUpsertWhenStorageUpdateFails()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var deviceModel = new DeviceModel { Id = id, ETag = "Etag" };
            this.TheModelExists(id, deviceModel);

            this.mockStorageRecords
                .Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>(), It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.UpsertAsync(deviceModel))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExternalDependencyExceptionWhenFailedFetchingDeviceModelInStorage()
        {
            // Arrange
            var deviceModel = new DeviceModel { Id = "id", ETag = "Etag" };

            // Act
            var ex = Record.Exception(() => this.target.UpsertAsync(deviceModel).Result);

            // Assert
            Assert.IsType<ExternalDependencyException>(ex.InnerException);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenDeleteDeviceModelFailed()
        {
            // Arrange
            this.mockStorageRecords
                .Setup(x => x.DeleteAsync(It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.DeleteAsync("someId"))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItFailsToGetDeviceModelsWhenStorageFails()
        {
            // Arrange
            this.mockStorageRecords
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.GetAsync("someId"))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenGetListOfDeviceModelsDeserializeFailed()
        {
            // Arrange
            this.SetupAListOfInvalidDeviceModelsInStorage();

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.GetListAsync())
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsAListOfCustomDeviceModels()
        {
            // Arrange
            const string UPDATED_ETAG = "etag";
            this.SetupAListOfDeviceModelsInStorage(UPDATED_ETAG);

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            foreach (var model in result)
            {
                Assert.Equal(DeviceModel.DeviceModelType.Custom, model.Type);
                Assert.Equal(UPDATED_ETAG, model.ETag);
            }
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenGetDeviceModelByInvalidId()
        {
            // Act & Assert
            Assert.ThrowsAsync<InvalidInputException>(
                    async () => await this.target.GetAsync(string.Empty))
                .Wait(Constants.TEST_TIMEOUT);
        }

        private void SetupAListOfDeviceModelsInStorage(string etag)
        {
            var deviceModel = new DeviceModel { Id = "id", ETag = etag };
            var list = new List<StorageRecord>();
            var document = new Document();
            document.Id = "key";
            document.SetPropertyValue("_etag", deviceModel.ETag);
            document.SetPropertyValue("Data", JsonConvert.SerializeObject(deviceModel));
            list.Add(StorageRecord.FromDocumentDb(document));

            this.mockStorageRecords
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(list);
        }

        private void SetupAListOfInvalidDeviceModelsInStorage()
        {
            var list = new List<StorageRecord>();
            var document = new Document();
            document.Id = "key";
            document.SetPropertyValue("_etag", "etag");
            document.SetPropertyValue("Data", JsonConvert.SerializeObject("{ 'invalid': json"));
            list.Add(StorageRecord.FromDocumentDb(document));

            this.mockStorageRecords
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(list);
        }

        private void ThereAreNoCustomDeviceModelsInStorage()
        {
            this.mockStorageRecords
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<StorageRecord>());
        }

        private void TheModelDoesntExist(string id)
        {
            this.mockStorageRecords
                .Setup(x => x.GetAsync(id))
                .Throws<ResourceNotFoundException>();
        }

        private void TheModelExists(string id, DeviceModel model)
        {
            this.mockStorageRecords
                .Setup(x => x.GetAsync(id))
                .ReturnsAsync(BuildStorageRecordList(model));
        }

        private StorageRecord BuildStorageRecordList(DeviceModel deviceModel)
        {
            // Create a mock DocumentDB Document object that will contain a
            // different ETag than the one we're trying to use to upsert
            var document = new Document();
            document.Id = deviceModel.Id;
            document.SetPropertyValue("_etag", deviceModel.ETag);
            document.SetPropertyValue("Data", JsonConvert.SerializeObject(deviceModel));
            return StorageRecord.FromDocumentDb(document);
        }
    }
}
