﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Moq;
using Services.Test.helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Services.Test
{
    public class DeviceModelsTest
    {
        private const string STORAGE_COLLECTION = "deviceModels";

        private readonly Mock<IStorageAdapterClient> storage;
        private readonly Mock<ILogger> logger;
        private readonly Mock<ICustomDeviceModels> customDeviceModels;
        private readonly Mock<IStockDeviceModels> stockDeviceModels;

        private readonly DeviceModels target;

        public DeviceModelsTest()
        {
            this.storage = new Mock<IStorageAdapterClient>();
            this.logger = new Mock<ILogger>();
            this.customDeviceModels = new Mock<ICustomDeviceModels>();
            this.stockDeviceModels = new Mock<IStockDeviceModels>();
            this.target = new DeviceModels(
                this.customDeviceModels.Object,
                this.stockDeviceModels.Object,
                this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsBothCustomAndStockDeviceModels()
        {
            // Arrange
            this.ThereAreThreeCustomDeviceModels();
            this.ThereAreThreeStockDeviceModels();
            const int TOTAL_MODEL_COUNT = 6;

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            Assert.Equal(TOTAL_MODEL_COUNT, result.Count());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsOnlyStockDeviceModels()
        {
            // Arrange
            this.ThereAreNoCustomDeviceModels();
            this.ThereAreThreeStockDeviceModels();
            const int TOTAL_MODEL_COUNT = 3;

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            Assert.Equal(TOTAL_MODEL_COUNT, result.Count());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsOnlyCustomDeviceModels()
        {
            // Arrange
            this.ThereAreThreeCustomDeviceModels();
            this.ThereAreNoStockDeviceModels();
            const int TOTAL_MODEL_COUNT = 3;

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            Assert.Equal(TOTAL_MODEL_COUNT, result.Count());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelById()
        {
            // Arrange
            this.ThereAreThreeCustomDeviceModels();
            this.ThereAreThreeStockDeviceModels();
            const string DEVICE_MODEL_ID = "chiller-01";

            // Act
            var result = this.target.GetAsync(DEVICE_MODEL_ID).Result;

            // Assert
            Assert.Equal(DEVICE_MODEL_ID, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsResourceNotFoundExceptionWhenDeviceModelNotFound()
        {
            // Arrange
            this.ThereAreThreeCustomDeviceModels();
            this.ThereAreThreeStockDeviceModels();

            // Act
            var ex = Record.Exception(() => this.target.GetAsync(It.IsAny<string>()).Result);

            // Assert
            Assert.IsType<ResourceNotFoundException>(ex.InnerException);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItInvokeCustomDeviceModelsServiceWhenDeleteDeviceModel()
        {
            // Act
            this.target.DeleteAsync(It.IsAny<string>()).Wait();

            // Assert
            this.customDeviceModels.Verify(
                x => x.DeleteAsync(It.IsAny<string>()));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task ItThrowsExceptionWhenDeleteDeviceModelFailed()
        {
            // Arrange
            this.customDeviceModels
                .Setup(x => x.DeleteAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception());

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => this.target.DeleteAsync(It.IsAny<string>()));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItInsertsDeviceModelToStorage()
        {
            // Arrange
            const string ETAG = "etag";
            var deviceModel = new DeviceModel() { ETag = ETAG };

            this.customDeviceModels
                .Setup(x => x.InsertAsync(It.IsAny<DeviceModel>(), true))
                .Returns(Task.FromResult(deviceModel));
            this.storage.Setup(x => x.UpdateAsync(
                    STORAGE_COLLECTION,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    null))
                .Returns(Task.FromResult(new ValueApiModel() { ETag = ETAG }));

            // Act
            var result = this.target.InsertAsync(deviceModel).Result;

            // Assert
            Assert.Equal(ETAG, result.ETag);
            Assert.Equal(result.Created, result.Modified);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task ItThrowsExternalDependencyExceptionWhenInsertDeviceModelFailed()
        {
            // Arrange
            this.customDeviceModels
                .Setup(x => x.InsertAsync(It.IsAny<DeviceModel>(), true))
                .ThrowsAsync(new Exception());

            // Act & Assert
            await Assert.ThrowsAsync<ExternalDependencyException>(() => this.target.InsertAsync(It.IsAny<DeviceModel>()));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItUpsertsDeviceModelInStorage()
        {
            // Arrange
            const string ETAG = "etag";
            var deviceModel = new DeviceModel() { ETag = ETAG };

            this.customDeviceModels
                .Setup(x => x.UpsertAsync(It.IsAny<DeviceModel>()))
                .Returns(Task.FromResult(deviceModel));
            this.storage.Setup(x => x.UpdateAsync(
                    STORAGE_COLLECTION,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    ETAG))
                .Returns(Task.FromResult(new ValueApiModel() { ETag = ETAG }));

            // Act
            var result = this.target.UpsertAsync(deviceModel).Result;

            // Assert
            Assert.Equal(ETAG, result.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task ItThrowsExternalDependencyExceptionWhenUpsertDeviceModelFailed()
        {
            // Arrange
            this.customDeviceModels
                .Setup(x => x.UpsertAsync(It.IsAny<DeviceModel>()))
                .ThrowsAsync(new Exception());

            // Act & Assert
            await Assert.ThrowsAsync<ExternalDependencyException>(() => this.target.UpsertAsync(It.IsAny<DeviceModel>()));
        }

        private void ThereAreNoCustomDeviceModels()
        {
            this.customDeviceModels
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(new List<DeviceModel>());
        }

        private void ThereAreNoStockDeviceModels()
        {
            this.stockDeviceModels
                .Setup(x => x.GetList())
                .Returns(new List<DeviceModel>());
        }

        private void ThereAreThreeStockDeviceModels()
        {
            var deviceModelsList = new List<DeviceModel>()
            {
                new DeviceModel() { Id = "chiller-01", ETag = "eTag_1"},
                new DeviceModel() { Id = "chiller-02", ETag = "eTag_2"},
                new DeviceModel() { Id = "chiller-03", ETag = "eTag_3"}
            };
            this.stockDeviceModels.Setup(x => x.GetList()).Returns(deviceModelsList);
        }

        private void ThereAreThreeCustomDeviceModels()
        {
            var deviceModelsList = new List<DeviceModel>()
            {
                new DeviceModel() { Id = "1", ETag = "eTag_1"},
                new DeviceModel() { Id = "2", ETag = "eTag_2"},
                new DeviceModel() { Id = "3", ETag = "eTag_3"}
            };

            this.customDeviceModels
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(deviceModelsList);
        }
    }
}
