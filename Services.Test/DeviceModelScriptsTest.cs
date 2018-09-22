// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Services.Test.helpers;
using Microsoft.Azure.Documents;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Services.Test
{
    public class DeviceModelScriptsTest
    {
        private readonly Mock<IServicesConfig> mockConfig;
        private readonly Mock<IFactory> mockFactory;
        private readonly Mock<IStorageRecords> mockStorageRecords;
        private readonly Mock<ILogger> mockLogger;
        private readonly DeviceModelScripts target;

        public DeviceModelScriptsTest()
        {
            this.mockConfig = new Mock<IServicesConfig>();
            this.mockFactory = new Mock<IFactory>();
            this.mockLogger = new Mock<ILogger>();

            this.target = new DeviceModelScripts(
                this.mockConfig.Object,
                this.mockFactory.Object,
                this.mockLogger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void InitialListIsEmpty()
        {
            // Arrange
            this.ThereAreNoDeviceModelScriptsInStorage();

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            Assert.Empty(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesDeviceModelScriptInStorage()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var eTag = Guid.NewGuid().ToString();
            var deviceModelScript = new DeviceModelScript { Id = id, ETag = eTag };

            this.mockStorageRecords
                .Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>(), It.IsAny<string>()))
                .ReturnsAsync(this.BuildStorageRecordList(deviceModelScript));

            // Act
            DeviceModelScript result = this.target.InsertAsync(deviceModelScript).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModelScript.Id, result.Id);
            Assert.Equal(deviceModelScript.ETag, result.ETag);

            this.mockStorageRecords.Verify(
                x => x.UpsertAsync( new StorageRecord { Id = deviceModelScript.Id, Data = It.IsAny<string>() }), Times.Once());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void DeviceModelScriptsCanBeUpserted()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();

            var deviceModelScript = new DeviceModelScript { Id = id, ETag = "oldEtag" };
            this.TheScriptExists(id, deviceModelScript);

            var updatedSimulationScript = new DeviceModelScript { Id = id, ETag = "newETag" };
            this.mockStorageRecords
                .Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>(), It.IsAny<string>()))
                .ReturnsAsync(this.BuildStorageRecordList(updatedSimulationScript));

            // Act
            this.target.UpsertAsync(deviceModelScript)
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

            Assert.Equal(updatedSimulationScript.Id, deviceModelScript.Id);
            // The call to UpsertAsync() modifies the object, so the ETags will match
            Assert.Equal(updatedSimulationScript.ETag, deviceModelScript.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesDeviceModelScriptWhenSimulationScriptNotFoundInUpserting()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var deviceModelScript = new DeviceModelScript { Id = id, ETag = "Etag" };
            this.TheScriptDoesntExist(id);
            this.mockStorageRecords
                .Setup(x => x.UpsertAsync(
                    new StorageRecord
                    {
                        Id = It.IsAny<string>(),
                        Data = It.IsAny<string>()
                    },
                    It.IsAny<string>()))
                .ReturnsAsync(this.BuildStorageRecordList(deviceModelScript));

            // Act
            this.target.UpsertAsync(deviceModelScript).Wait(TimeSpan.FromSeconds(30));

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
            var deviceModelScriptInStorage = new DeviceModelScript { Id = id, ETag = "ETag" };
            this.TheScriptExists(id, deviceModelScriptInStorage);

            // Act & Assert
            var deviceModelScript = new DeviceModelScript { Id = id, ETag = "not-matching-Etag" };
            Assert.ThrowsAsync<ConflictingResourceException>(
                    async () => await this.target.UpsertAsync(deviceModelScript))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenInsertDeviceModelScriptFailed()
        {
            // Arrange
            var deviceModelScript = new DeviceModelScript { Id = "id", ETag = "Etag" };
            this.mockStorageRecords
                .Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>(), It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.InsertAsync(deviceModelScript))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItFailsToUpsertWhenUnableToFetchScriptFromStorage()
        {
            // Arrange
            var deviceModelScript = new DeviceModelScript { Id = "id", ETag = "Etag" };
            this.mockStorageRecords
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.UpsertAsync(deviceModelScript))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItFailsToUpsertWhenStorageUpdateFails()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var deviceModelScript = new DeviceModelScript { Id = id, ETag = "Etag" };
            this.TheScriptExists(id, deviceModelScript);

            this.mockStorageRecords
                .Setup(x => x.UpsertAsync(It.IsAny<StorageRecord>(), It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.UpsertAsync(deviceModelScript))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExternalDependencyExceptionWhenFailedFetchingDeviceModelScriptInStorage()
        {
            // Arrange
            var deviceModelScript = new DeviceModelScript { Id = "id", ETag = "Etag" };

            // Act
            var ex = Record.Exception(() => this.target.UpsertAsync(deviceModelScript).Result);

            // Assert
            Assert.IsType<ExternalDependencyException>(ex.InnerException);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenDeleteDeviceModelScriptFailed()
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
        public void ItFailsToGetDeviceModelScriptsWhenStorageFails()
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
        public void ItThrowsExceptionWhenGetListOfDeviceModelScriptsDeserializeFailed()
        {
            // Arrange
            this.SetupAListOfInvalidDeviceModelScriptsInStorage();

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.GetListAsync())
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenGetDeviceModelScriptByInvalidId()
        {
            // Act & Assert
            Assert.ThrowsAsync<InvalidInputException>(
                    async () => await this.target.GetAsync(string.Empty))
                .Wait(Constants.TEST_TIMEOUT);
        }

        private void SetupAListOfInvalidDeviceModelScriptsInStorage()
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

        private void TheScriptDoesntExist(string id)
        {
            this.mockStorageRecords
                .Setup(x => x.GetAsync(id))
                .Throws<ResourceNotFoundException>();
        }

        private void TheScriptExists(string id, DeviceModelScript deviceModelScript)
        {
            this.mockStorageRecords
                .Setup(x => x.GetAsync(id))
                .ReturnsAsync(this.BuildStorageRecordList(deviceModelScript));
        }

        private StorageRecord BuildStorageRecordList(DeviceModelScript deviceModelScript)
        {
            // Create a mock DocumentDB Document object that will contain a
            // different ETag than the one we're trying to use to upsert
            var document = new Document();
            document.Id = deviceModelScript.Id;
            document.SetPropertyValue("_etag", deviceModelScript.ETag);
            document.SetPropertyValue("Data", JsonConvert.SerializeObject(deviceModelScript));
            return StorageRecord.FromDocumentDb(document);
        }

        private void ThereAreNoDeviceModelScriptsInStorage()
        {
            this.mockStorageRecords
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<StorageRecord>());
        }
    }
}
