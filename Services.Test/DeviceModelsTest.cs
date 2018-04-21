// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Moq;
using Services.Test.helpers;
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
        private readonly Mock<IServicesConfig> config;
        private readonly Mock<ILogger> logger;
        private readonly Mock<ICustomDeviceModels> customDeviceModels;
        private readonly Mock<IStockDeviceModels> stockDeviceModels;

        private readonly DeviceModels target;

        public DeviceModelsTest()
        {
            this.storage = new Mock<IStorageAdapterClient>();
            this.config = new Mock<IServicesConfig>();
            this.logger = new Mock<ILogger>();
            this.customDeviceModels = new Mock<ICustomDeviceModels>();
            this.stockDeviceModels = new Mock<IStockDeviceModels>();
            this.target = new DeviceModels(
                this.storage.Object,
                this.customDeviceModels.Object,
                this.stockDeviceModels.Object,
                this.config.Object,
                this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsBothCustomAndStockDeviceModels()
        {
            // Arrange
            this.ThereAreThreeCustomDeviceModels();
            this.ThereAreTenStockDeviceModels();
            const int totalModelCount = 13;

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            Assert.Equal(totalModelCount, result.Count());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsOnlyStockDeviceModels()
        {
            // Arrange
            this.ThereAreNoCustomDeviceModels();
            this.ThereAreTenStockDeviceModels();
            const int totalModelCount = 10;

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            Assert.Equal(totalModelCount, result.Count());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsOnlyCustomDeviceModels()
        {
            // Arrange
            this.ThereAreThreeCustomDeviceModels();
            this.ThereAreNoStockDeviceModels();
            const int totalModelCount = 3;

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            Assert.Equal(totalModelCount, result.Count());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelById()
        {
            // Arrange
            this.ThereAreThreeCustomDeviceModels();
            this.ThereAreTenStockDeviceModels();
            const string deviceModelId = "chiller-01";

            // Act
            var result = this.target.GetAsync(deviceModelId).Result;

            // Assert
            Assert.Equal(deviceModelId, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsResourceNotFoundExceptionWhenDeviceModelNotFound()
        {
            // Arrange
            this.ThereAreThreeCustomDeviceModels();
            this.ThereAreTenStockDeviceModels();

            // Act
            var ex = Record.Exception(() => this.target.GetAsync(It.IsAny<string>()).Result);

            // Assert
            Assert.IsType<ResourceNotFoundException>(ex.InnerException);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItInvokeStorageServiceWhenDeleteDeviceModel()
        {
            // Arrange
            this.ThereAreThreeCustomDeviceModels();
            this.ThereAreTenStockDeviceModels();

            // Act
            this.target.DeleteAsync(It.IsAny<string>()).Wait();

            // Assert
            this.storage.Verify(
                x => x.DeleteAsync(STORAGE_COLLECTION, It.IsAny<string>()));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItInsertsDeviceModelToStorage()
        {
            // Arrange
            const string etag = "etag";
            var deviceModel = new DeviceModel() { ETag = etag };

            this.customDeviceModels
                .Setup(x => x.InsertAsync(It.IsAny<DeviceModel>()))
                .Returns(Task.FromResult(deviceModel));
            this.storage.Setup(x => x.UpdateAsync(
                    STORAGE_COLLECTION,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    null))
                .Returns(Task.FromResult(new ValueApiModel() { ETag = etag }));

            // Act
            var result = this.target.InsertAsync(deviceModel).Result;

            // Assert
            Assert.Equal(etag, result.ETag);
            Assert.Equal(result.Created, result.Modified);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItUpsertsDeviceModelInStorage()
        {
            // Arrange
            const string etag = "etag";
            var deviceModel = new DeviceModel() { ETag = etag };

            this.customDeviceModels
                .Setup(x => x.UpsertAsync(It.IsAny<DeviceModel>()))
                .Returns(Task.FromResult(deviceModel));
            this.storage.Setup(x => x.UpdateAsync(
                    STORAGE_COLLECTION,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    etag))
                .Returns(Task.FromResult(new ValueApiModel() { ETag = etag }));

            // Act
            var result = this.target.InsertAsync(deviceModel).Result;

            // Assert
            Assert.Equal(etag, result.ETag);
        }

        private void ThereAreNoCustomDeviceModels()
        {
            this.storage
                .Setup(x => x.GetAllAsync(STORAGE_COLLECTION))
                .ReturnsAsync(new ValueListApiModel());
        }

        private void ThereAreNoStockDeviceModels()
        {
            this.config.Setup(x => x.DeviceModelsFolder).Returns("./data/");
        }

        private void ThereAreTenStockDeviceModels()
        {
            this.config.Setup(x => x.DeviceModelsFolder).Returns("./data/devicemodels/");
        }

        private void ThereAreThreeCustomDeviceModels()
        {
            var customDeviceAPIModels = new ValueListApiModel()
            {
                Items = new List<ValueApiModel>()
                {
                    new ValueApiModel() { Data = "{\"ETag\":\"\",\"Id\":\"custom_01\"}" },
                    new ValueApiModel() { Data = "{\"ETag\":\"\",\"Id\":\"custom_02\"}" },
                    new ValueApiModel() { Data = "{\"ETag\":\"\",\"Id\":\"custom_03\"}" }
                }
            };

            this.storage
                .Setup(x => x.GetAllAsync(STORAGE_COLLECTION))
                .ReturnsAsync(customDeviceAPIModels);
        }
    }
}
