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
            this.ThereAreNoCustomDeviceModelsInTheStorage();

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            Assert.Empty(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreateCustomDeviceModel()
        {
            // Arrange
            var deviceModel = new DeviceModel { ETag = "Etag_1" };
            this.UpdateDeviceModelInStorage(deviceModel);

            // Act
            DeviceModel result = this.target.InsertAsync(deviceModel).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModel.ETag, result.ETag);
        }


        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreatedCustomDeviceModelsAreStored()
        {
            // Arrange
            var deviceModel = new DeviceModel { ETag = "Etag_1" };
            this.UpdateDeviceModelInStorage(deviceModel);

            // Act
            this.target.InsertAsync(deviceModel).Wait();

            // Assert
            this.storage.Verify(x => x.UpdateAsync(
                STORAGE_COLLECTION,
                It.IsAny<string>(),
                It.IsAny<string>(),
                null));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CustomDeviceModelsCanBeUpserted()
        {
            // Arrange
            var deviceModel = new DeviceModel { ETag = "oldEtag" };
            var updatedDeviceModel = new DeviceModel  { ETag = "newETag" };

            this.SaveDeviceModelInStorage(deviceModel);
            this.UpdateDeviceModelInStorage(updatedDeviceModel);

            // Act
            this.target.UpsertAsync(deviceModel).Wait();

            // Assert
            this.storage.Verify(x => x.UpdateAsync(
                STORAGE_COLLECTION,
                It.IsAny<string>(),
                It.IsAny<string>(),
                "oldEtag"));

            Assert.Equal("newETag", deviceModel.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CreateNewCustomDeviceModelsIfNotExisted()
        {
            // Arrange
            var deviceModel = new DeviceModel { Id = "id", ETag = "Etag" };

            this.ThereIsNoCustomDeviceModelsByIdInTheStorage(deviceModel.Id);
            this.UpdateDeviceModelInStorage(deviceModel);

            // Act
            this.target.UpsertAsync(deviceModel).Wait();

            // Assert
            this.storage.Verify(x => x.UpdateAsync(
                STORAGE_COLLECTION,
                It.IsAny<string>(),
                It.IsAny<string>(),
                null));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CustomDeviceModelsCanNotBeUpsertedIfEtagNotMatch()
        {
            // Arrange
            var deviceModel = new DeviceModel { ETag = "oldEtag" };
            var updatedDeviceModel = new DeviceModel { ETag = "newETag" };

            this.SaveDeviceModelInStorage(deviceModel);
            this.UpdateDeviceModelInStorage(updatedDeviceModel);

            // Act + Assert
            var modifiedEtagModel = new DeviceModel { ETag = "Non-exist-Etag" };
            Assert.ThrowsAsync<ConflictingResourceException>(() => this.target.UpsertAsync(modifiedEtagModel));
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

        private void ThereAreNoCustomDeviceModelsInTheStorage()
        {
            this.storage
                .Setup(x => x.GetAllAsync(STORAGE_COLLECTION))
                .ReturnsAsync(new ValueListApiModel());
        }

        private void ThereIsNoCustomDeviceModelsByIdInTheStorage(string id)
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
