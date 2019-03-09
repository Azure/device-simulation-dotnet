// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Moq;
using Newtonsoft.Json;
using Services.Test.helpers;
using Xunit;

namespace Services.Test
{
    public class DeviceModelScriptsTest
    {
        private const string STORAGE_COLLECTION = "deviceModelScripts";
        private readonly Mock<IStorageAdapterClient> storage;
        private readonly Mock<ILogger> logger;
        private readonly DeviceModelScripts target;

        public DeviceModelScriptsTest()
        {
            this.storage = new Mock<IStorageAdapterClient>();
            this.logger = new Mock<ILogger>();

            this.target = new DeviceModelScripts(
                this.storage.Object,
                this.logger.Object);
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
            var deviceModelScript = new DataFile { Id = id, ETag = eTag };

            this.storage
                .Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(this.BuildValueApiModel(deviceModelScript));

            // Act
            DataFile result = this.target.InsertAsync(deviceModelScript).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModelScript.Id, result.Id);
            Assert.Equal(deviceModelScript.ETag, result.ETag);

            this.storage.Verify(
                x => x.UpdateAsync(STORAGE_COLLECTION, deviceModelScript.Id, It.IsAny<string>(), null), Times.Once());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void DeviceModelScriptsCanBeUpserted()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();

            var deviceModelScript = new DataFile { Id = id, ETag = "oldEtag" };
            this.TheScriptExists(id, deviceModelScript);

            var updatedSimulationScript = new DataFile { Id = id, ETag = "newETag" };
            this.storage
                .Setup(x => x.UpdateAsync(
                    STORAGE_COLLECTION,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(this.BuildValueApiModel(updatedSimulationScript));

            // Act
            this.target.UpsertAsync(deviceModelScript)
                .Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.storage.Verify(x => x.GetAsync(STORAGE_COLLECTION, id), Times.Once);
            this.storage.Verify(x => x.UpdateAsync(
                STORAGE_COLLECTION,
                id,
                It.Is<string>(json => JsonConvert.DeserializeObject<DataFile>(json).Id == id && !json.Contains("ETag")),
                "oldEtag"), Times.Once());

            Assert.Equal(updatedSimulationScript.Id, deviceModelScript.Id);
            // The call to UpsertAsync() modifies the object, so the ETags will match
            Assert.Equal(updatedSimulationScript.ETag, deviceModelScript.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesDeviceModelScriptWhenSimulationScriptNotFoundInUpserting()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var deviceModelScript = new DataFile { Id = id, ETag = "Etag" };
            this.TheScriptDoesntExist(id);
            this.storage
                .Setup(x => x.UpdateAsync(
                    STORAGE_COLLECTION,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(this.BuildValueApiModel(deviceModelScript));

            // Act
            this.target.UpsertAsync(deviceModelScript).Wait(TimeSpan.FromSeconds(30));

            // Assert - the app uses PUT with given ID
            this.storage.Verify(x => x.UpdateAsync(
                STORAGE_COLLECTION,
                id,
                It.IsAny<string>(),
                null));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsConflictingResourceExceptionIfEtagDoesNotMatchInUpserting()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var deviceModelScriptInStorage = new DataFile { Id = id, ETag = "ETag" };
            this.TheScriptExists(id, deviceModelScriptInStorage);

            // Act & Assert
            var deviceModelScript = new DataFile { Id = id, ETag = "not-matching-Etag" };
            Assert.ThrowsAsync<ConflictingResourceException>(
                    async () => await this.target.UpsertAsync(deviceModelScript))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenInsertDeviceModelScriptFailed()
        {
            // Arrange
            var deviceModelScript = new DataFile { Id = "id", ETag = "Etag" };
            this.storage
                .Setup(x => x.UpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
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
            var deviceModelScript = new DataFile { Id = "id", ETag = "Etag" };
            this.storage
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
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
            var deviceModelScript = new DataFile { Id = id, ETag = "Etag" };
            this.TheScriptExists(id, deviceModelScript);

            this.storage
                .Setup(x => x.UpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
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
            var deviceModelScript = new DataFile { Id = "id", ETag = "Etag" };

            // Act
            var ex = Record.Exception(() => this.target.UpsertAsync(deviceModelScript).Result);

            // Assert
            Assert.IsType<ExternalDependencyException>(ex.InnerException);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenDeleteDeviceModelScriptFailed()
        {
            // Arrange
            this.storage
                .Setup(x => x.DeleteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
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
            this.storage
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
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
            var list = new ValueListApiModel();
            var value = new ValueApiModel
            {
                Key = "key1",
                Data = "{ 'invalid': json",
                ETag = "etag"
            };
            list.Items.Add(value);

            this.storage
                .Setup(x => x.GetAllAsync(It.IsAny<string>()))
                .ReturnsAsync(list);
        }

        private void TheScriptDoesntExist(string id)
        {
            this.storage
                .Setup(x => x.GetAsync(STORAGE_COLLECTION, id))
                .Throws<ResourceNotFoundException>();
        }

        private void TheScriptExists(string id, DataFile deviceModelScript)
        {
            this.storage
                .Setup(x => x.GetAsync(STORAGE_COLLECTION, id))
                .ReturnsAsync(this.BuildValueApiModel(deviceModelScript));
        }

        private ValueApiModel BuildValueApiModel(DataFile deviceModelScript)
        {
            return new ValueApiModel
            {
                Key = deviceModelScript.Id,
                Data = JsonConvert.SerializeObject(deviceModelScript),
                ETag = deviceModelScript.ETag
            };
        }

        private void ThereAreNoDeviceModelScriptsInStorage()
        {
            this.storage
                .Setup(x => x.GetAllAsync(STORAGE_COLLECTION))
                .ReturnsAsync(new ValueListApiModel());
        }
    }
}
