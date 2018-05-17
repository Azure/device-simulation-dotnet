// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Moq;
using Newtonsoft.Json;
using Services.Test.helpers;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Services.Test
{
    public class CustomDeviceModelsTest
    {
        private const string STORAGE_COLLECTION = "deviceModels";

        private readonly ITestOutputHelper log;
        private readonly Mock<IStorageAdapterClient> storage;
        private readonly Mock<ILogger> logger;
        private readonly CustomDeviceModels target;

        public CustomDeviceModelsTest(ITestOutputHelper log)
        {
            this.log = log;

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
        public void ItCreatesCustomDeviceModelsWithProvidedIdInStorage()
        {
            // Arrange
            var deviceModel = new DeviceModel { Id = "id", ETag = "Etag_1" };
            this.UpdateDeviceModelInStorage(deviceModel);

            // Act
            DeviceModel result = this.target.InsertAsync(deviceModel, false).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModel.Id, result.Id);
            Assert.Equal(deviceModel.ETag, result.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesCustomDeviceModelsWithGuidInStorage()
        {
            // Arrange
            var deviceModel = new DeviceModel { ETag = "Etag_1" };
            this.UpdateDeviceModelInStorage(deviceModel);

            // Act
            DeviceModel result = this.target.InsertAsync(deviceModel).Result;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Id);
            Assert.Equal(deviceModel.ETag, result.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreatedCustomDeviceModelsAreStored()
        {
            // Arrange
            var deviceModel = new DeviceModel { ETag = "Etag_1" };
            this.UpdateDeviceModelInStorage(deviceModel);

            // Act
            this.target.InsertAsync(deviceModel).Wait(TimeSpan.FromSeconds(30));

            // Assert
            this.storage.Verify(x => x.UpdateAsync(
                STORAGE_COLLECTION,
                It.IsAny<string>(),
                It.IsAny<string>(),
                null), Times.Once());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CustomDeviceModelsCanBeUpserted()
        {
            // Arrange
            var deviceModel = new DeviceModel { Id = "id", ETag = "oldEtag" };
            var updatedDeviceModel = new DeviceModel  { Id = "id", ETag = "newETag" };

            this.SaveDeviceModelInStorage(deviceModel);
            this.UpdateDeviceModelInStorage(updatedDeviceModel);

            // Act
            this.target.UpsertAsync(deviceModel).Wait();

            // Assert
            this.storage.Verify(x => x.UpdateAsync(
                STORAGE_COLLECTION,
                It.IsAny<string>(),
                It.IsAny<string>(),
                "oldEtag"), Times.Once());

            Assert.Equal(updatedDeviceModel.Id, deviceModel.Id);
            Assert.Equal(updatedDeviceModel.ETag, deviceModel.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesDeviceModelWhenDeviceModelNotFoundInUpserting()
        {
            // Arrange
            var deviceModel = new DeviceModel { Id = "id", ETag = "Etag" };

            this.ThereIsNoCustomDeviceModelByIdInStorage(deviceModel.Id);
            this.UpdateDeviceModelInStorage(deviceModel);

            // Act
            this.target.UpsertAsync(deviceModel).Wait(TimeSpan.FromSeconds(30));

            // Assert
            this.storage.Verify(x => x.UpdateAsync(
                STORAGE_COLLECTION,
                It.IsAny<string>(),
                It.IsAny<string>(),
                null));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsConflictingResourceExceptionIfEtagDoesNotMatchInUpserting()
        {
            // Arrange
            var deviceModelInStorage = new DeviceModel { ETag = "ETag" };
            var deviceModel = new DeviceModel { ETag = "not-matching-Etag" };
            
            this.UpdateDeviceModelInStorage(deviceModelInStorage);

            // Act & Assert
            Assert.ThrowsAsync<ConflictingResourceException>(() => this.target.UpsertAsync(deviceModel));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenInsertDeviceModelFailed()
        {
            // Arrange
            this.SetupStorageToThrowException();

            // Act & Assert
            Assert.ThrowsAsync<Exception>(() => this.target.InsertAsync(It.IsAny<DeviceModel>()));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenUpsertDeviceModelFailed()
        {
            // Arrange
            this.SetupStorageToThrowException();

            // Act & Assert
            Assert.ThrowsAsync<Exception>(() => this.target.UpsertAsync(It.IsAny<DeviceModel>()));
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
                .ThrowsAsync(new Exception());

            // Act & Assert
            Assert.ThrowsAsync<Exception>(() => this.target.DeleteAsync(It.IsAny<string>()));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExternalDependencyExceptionWhenGetAllDeviceModelFailed()
        {
            // Arrange
            this.storage
                .Setup(x => x.GetAllAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(() => this.target.DeleteAsync(It.IsAny<string>()));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenGetListOfDeviceModelsDeserializeFailed()
        {
            // Arrange
            this.SetupAListOfInvalidDeviceModelsInStorage();

            // Act & Assert
            Assert.ThrowsAsync<Exception>(() => this.target.GetListAsync());
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
                Assert.Equal("CustomModel", model.Type);
                Assert.Equal(UPDATED_ETAG, model.ETag);
            }
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenGetDeviceModelByIdFailed()
        {
            // Act & Assert
            Assert.ThrowsAsync<Exception>(() => this.target.GetAsync(String.Empty));
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
            var obj = new { id = "id", eTag = "Etag" };
            var list = new ValueListApiModel();
            var value = new ValueApiModel
            {
                Key = "key",
                Data = JsonConvert.SerializeObject(obj),
                ETag = "etag"
            };
            list.Items.Add(value);

            this.storage
                .Setup(x => x.GetAllAsync(It.IsAny<string>()))
                .ReturnsAsync(list);
        }

        private void SetupStorageToThrowException()
        {
            this.storage
                .Setup(x => x.UpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception());
        }

        private void SaveDeviceModelInStorage(DeviceModel deviceModel)
        {
            var result = new ValueApiModel
            {
                ETag = deviceModel.ETag,
                Data = JsonConvert.SerializeObject(deviceModel)
            };

            this.storage
                .Setup(x => x.GetAsync(STORAGE_COLLECTION, It.IsAny<string>()))
                .ReturnsAsync(result);
        }

        private void ThereAreNoCustomDeviceModelsInStorage()
        {
            this.storage
                .Setup(x => x.GetAllAsync(STORAGE_COLLECTION))
                .ReturnsAsync(new ValueListApiModel());
        }

        private void ThereIsNoCustomDeviceModelByIdInStorage(string id)
        {
            this.storage
                .Setup(x => x.GetAsync(STORAGE_COLLECTION, id))
                .Throws<ResourceNotFoundException>();
        }

        private void UpdateDeviceModelInStorage(DeviceModel deviceModel)
        {
            var updatedValue = new ValueApiModel
            {
                Key = deviceModel.Id,
                Data = JsonConvert.SerializeObject(deviceModel),
                ETag = deviceModel.ETag
            };

            this.storage
                .Setup(x => x.UpdateAsync(
                    STORAGE_COLLECTION,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(updatedValue);
        }
    }
}
