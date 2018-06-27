// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Moq;
using Newtonsoft.Json.Linq;
using Services.Test.helpers;
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

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            Assert.Equal(6, result.Count());
            Assert.Equal(3, result.Count(x => x.Type == DeviceModel.DeviceModelType.Custom));
            Assert.Equal(3, result.Count(x => x.Type == DeviceModel.DeviceModelType.Stock));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsOnlyStockDeviceModelsIfThereAreNoCustomModels()
        {
            // Arrange
            this.ThereAreNoCustomDeviceModels();
            this.ThereAreThreeStockDeviceModels();
            const int TOTAL_MODEL_COUNT = 3;

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            Assert.Equal(TOTAL_MODEL_COUNT, result.Count());
            Assert.Equal(TOTAL_MODEL_COUNT, result.Count(x => x.Type == DeviceModel.DeviceModelType.Stock));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsOnlyCustomDeviceModelsIfThereAreNoStockModels()
        {
            // Arrange
            this.ThereAreThreeCustomDeviceModels();
            this.ThereAreNoStockDeviceModels();
            const int TOTAL_MODEL_COUNT = 3;

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            Assert.Equal(TOTAL_MODEL_COUNT, result.Count());
            Assert.Equal(TOTAL_MODEL_COUNT, result.Count(x => x.Type == DeviceModel.DeviceModelType.Custom));
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
        public void ItReturnsPropertyNamesOfDeviceModels()
        {
            // Arrange
            var properties = new Dictionary<string, object>();
            properties.Add("Type", "chiller");
            properties.Add("Firmware", "1.0");
            properties.Add("Location", "Building 2");
            properties.Add("Model", "CH101");
            var deviceModels = this.GetDeviceModelsWithProperties(properties);
            this.customDeviceModels
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(deviceModels);

            // Act
            var result = this.target.GetPropertyNamesAsync().Result;

            // Assert
            Assert.Equal(properties.Count, result.Count);
            foreach (var prop in result)
            {
                Assert.True(properties.ContainsKey(prop.Split('.')[2]));
            }
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
        public void ItThrowsExceptionWhenDeleteDeviceModelFailed()
        {
            /* SomeException is required to verify that the exception thrown is the one 
             * configured in Arrange expression, and the test doesn't pass for the wrong 
             * reason. That's why we use a helper class SomeException which
             * doesn't exist in the application. Configuring GetPropertyNameAsync to
             * throw SomeException allows us to verify that the exception thrown is
             * exactly SomeException and not something else. */

            // Arrange
            this.customDeviceModels
                .Setup(x => x.DeleteAsync(It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<SomeException>(async () => await this.target.DeleteAsync(It.IsAny<string>()))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItInsertsDeviceModelToStorage()
        {
            // Arrange
            const string ETAG = "etag";
            var deviceModel = new DeviceModel { ETag = ETAG };

            this.customDeviceModels
                .Setup(x => x.InsertAsync(It.IsAny<DeviceModel>(), true))
                .Returns(Task.FromResult(deviceModel));
            this.storage.Setup(x => x.UpdateAsync(
                    STORAGE_COLLECTION,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    null))
                .Returns(Task.FromResult(new ValueApiModel { ETag = ETAG }));

            // Act
            var result = this.target.InsertAsync(deviceModel).Result;

            // Assert
            Assert.Equal(ETAG, result.ETag);
            Assert.Equal(result.Created, result.Modified);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenInsertDeviceModelFailed()
        {
            // Arrange
            var deviceModel = new DeviceModel { Id = "id" };

            this.customDeviceModels
                .Setup(x => x.InsertAsync(It.IsAny<DeviceModel>(), true))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<SomeException>(async () => await this.target.InsertAsync(deviceModel))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItUpsertsDeviceModelInStorage()
        {
            // Arrange
            const string ETAG = "etag";
            var deviceModel = new DeviceModel { ETag = ETAG };

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
        public void ItThrowsExceptionWhenUpsertDeviceModelFailed()
        {
            // Arrange
            var deviceModel = new DeviceModel { Id = "id" };

            this.customDeviceModels
                .Setup(x => x.UpsertAsync(It.IsAny<DeviceModel>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<SomeException>(async () => await this.target.UpsertAsync(deviceModel))
                .Wait(Constants.TEST_TIMEOUT);
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
            var deviceModelsList = new List<DeviceModel>
            {
                new DeviceModel { Id = "chiller-01", ETag = "eTag_1", Type = DeviceModel.DeviceModelType.Stock },
                new DeviceModel { Id = "chiller-02", ETag = "eTag_2", Type = DeviceModel.DeviceModelType.Stock },
                new DeviceModel { Id = "chiller-03", ETag = "eTag_3", Type = DeviceModel.DeviceModelType.Stock }
            };
            this.stockDeviceModels.Setup(x => x.GetList()).Returns(deviceModelsList);
        }

        private void ThereAreThreeCustomDeviceModels()
        {
            var deviceModelsList = new List<DeviceModel>
            {
                new DeviceModel { Id = "1", ETag = "eTag_1", Type = DeviceModel.DeviceModelType.Custom },
                new DeviceModel { Id = "2", ETag = "eTag_2", Type = DeviceModel.DeviceModelType.Custom },
                new DeviceModel { Id = "3", ETag = "eTag_3", Type = DeviceModel.DeviceModelType.Custom }
            };

            this.customDeviceModels
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(deviceModelsList);
        }

        private List<DeviceModel> GetDeviceModelsWithProperties(Dictionary<string, object> properties)
        {
            var deviceModels = new List<DeviceModel>
            {
                new DeviceModel {
                    Id = "Id_1",
                    Properties =  properties
                },
                new DeviceModel {
                    Id = "Id_2",
                    Properties =  properties
                }
            };

            return deviceModels;
        }
    }
}
