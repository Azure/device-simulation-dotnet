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
    public class CustomDeviceModelsTest
    {
        private const string STORAGE_COLLECTION = "deviceModels";

        private readonly Mock<IStorageAdapterClient> storage;
        private readonly Mock<ILogger> logger;
        private readonly CustomDeviceModels target;

        public CustomDeviceModelsTest()
        {
            this.storage = new Mock<IStorageAdapterClient>();
            this.logger = new Mock<ILogger>();

            this.target = new CustomDeviceModels(
                this.storage.Object,
                this.logger.Object);
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

            this.storage
                .Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(BuildValueApiModel(deviceModel));

            // Act
            DeviceModel result = this.target.InsertAsync(deviceModel, false).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModel.Id, result.Id);
            Assert.Equal(deviceModel.ETag, result.ETag);
            this.storage.Verify(x => x.UpdateAsync(
                STORAGE_COLLECTION,
                deviceModel.Id,
                It.Is<string>(json => JsonConvert.DeserializeObject<DeviceModel>(json).Id == id && !json.Contains("ETag")),
                null), Times.Once());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CustomDeviceModelsCanBeUpserted()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();

            var oldDeviceModel = new DeviceModel { Id = id, ETag = "oldEtag" };
            this.TheModelExists(id, oldDeviceModel);

            var updatedDeviceModel = new DeviceModel { Id = id, ETag = "newETag" };
            this.storage
                .Setup(x => x.UpdateAsync(
                    STORAGE_COLLECTION,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(BuildValueApiModel(updatedDeviceModel));

            // Act
            this.target.UpsertAsync(oldDeviceModel)
                .Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.storage.Verify(x => x.GetAsync(STORAGE_COLLECTION, id), Times.Once);
            this.storage.Verify(x => x.UpdateAsync(
                STORAGE_COLLECTION,
                id,
                It.Is<string>(json => JsonConvert.DeserializeObject<DeviceModel>(json).Id == id && !json.Contains("ETag")),
                "oldEtag"), Times.Once());

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
            this.storage
                .Setup(x => x.UpdateAsync(
                    STORAGE_COLLECTION,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(BuildValueApiModel(deviceModel));

            // Act
            this.target.UpsertAsync(deviceModel).Wait(TimeSpan.FromSeconds(30));

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
            this.storage
                .Setup(x => x.UpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
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
            this.storage
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
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

            this.storage
                .Setup(x => x.UpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
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
        public void ItFailsToGetDeviceModelsWhenStorageFails()
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
            var list = new ValueListApiModel();
            var value = new ValueApiModel
            {
                Key = "key",
                Data = JsonConvert.SerializeObject(deviceModel),
                ETag = deviceModel.ETag
            };
            list.Items.Add(value);

            this.storage
                .Setup(x => x.GetAllAsync(It.IsAny<string>()))
                .ReturnsAsync(list);
        }

        private void SetupAListOfInvalidDeviceModelsInStorage()
        {
            var list = new ValueListApiModel();
            var value = new ValueApiModel
            {
                Key = "key",
                Data = "{ 'invalid': json",
                ETag = "etag"
            };
            list.Items.Add(value);

            this.storage
                .Setup(x => x.GetAllAsync(It.IsAny<string>()))
                .ReturnsAsync(list);
        }

        private void ThereAreNoCustomDeviceModelsInStorage()
        {
            this.storage
                .Setup(x => x.GetAllAsync(STORAGE_COLLECTION))
                .ReturnsAsync(new ValueListApiModel());
        }

        private void TheModelDoesntExist(string id)
        {
            this.storage
                .Setup(x => x.GetAsync(STORAGE_COLLECTION, id))
                .Throws<ResourceNotFoundException>();
        }

        private void TheModelExists(string id, DeviceModel model)
        {
            this.storage
                .Setup(x => x.GetAsync(STORAGE_COLLECTION, id))
                .ReturnsAsync(BuildValueApiModel(model));
        }

        private static ValueApiModel BuildValueApiModel(DeviceModel model)
        {
            return new ValueApiModel
            {
                Key = model.Id,
                Data = JsonConvert.SerializeObject(model),
                ETag = model.ETag
            };
        }
    }
}
