// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Moq;
using Services.Test.helpers;
using System;
using System.IO;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Xunit;

namespace Services.Test
{
    public class StockDeviceModelTest
    {
        private readonly Mock<ILogger> logger;
        private readonly Mock<IServicesConfig> config;
        private readonly StockDeviceModels target;

        public StockDeviceModelTest()
        {
            this.logger = new Mock<ILogger>();
            this.config = new Mock<IServicesConfig>();

            this.target = new StockDeviceModels(
                this.config.Object,
                this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsStockDeviceModelsFromFileSystem()
        {
            // Arrange
            this.config.Setup(x => x.DeviceModelsFolder).Returns("./data/devicemodels/");
            // Note, based on current setup, simulation service has 10 stock models available.
            const int STOCK_MODEL_COUNT = 10;

            // Act
            var result = this.target.GetList();

            // Assert
            Assert.Equal(STOCK_MODEL_COUNT, result.Count());
            foreach (var model in result)
            {
                Assert.Equal(DeviceModel.DeviceModelType.Stock, model.Type);
            }
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsDirectoryNotFoundExceptionWhenLoadDeviceModelFilesFailed()
        {
            // Arrange
            this.config.Setup(x => x.DeviceModelsFolder).Returns("./fake/path/");

            // Act
            var ex = Record.Exception(() => this.target.GetList());

            // Assert
            Assert.IsType<DirectoryNotFoundException>(ex);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsStockDeviceModelById()
        {
            // Arrange
            this.config.Setup(x => x.DeviceModelsFolder).Returns("./data/devicemodels/");
            const string STOCK_MODEL_ID = "chiller-01";

            // Act
            var result = this.target.Get(STOCK_MODEL_ID);

            // Assert
            Assert.Equal(STOCK_MODEL_ID, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsResourceNotFoundExceptionWhenStockDeviceModelNotFound()
        {
            // Arrange
            this.config.Setup(x => x.DeviceModelsFolder).Returns("./data/devicemodels/");
            const string STOCK_MODEL_ID = "fake_id";

            // Act
            var ex = Record.Exception(() => this.target.Get(STOCK_MODEL_ID));

            // Assert
            Assert.IsType<ResourceNotFoundException>(ex);
        }
    }
}
