// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Controllers;
using Moq;
using WebService.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace WebService.Test.v2.Controllers
{
    public class DeviceModelsControllerTest
    {
        private readonly Mock<IDeviceModels> deviceModelsService;
        private readonly Mock<ILogger> logger;
        private readonly DeviceModelsController target;

        public DeviceModelsControllerTest(ITestOutputHelper lo)
        {
            this.deviceModelsService = new Mock<IDeviceModels>();
            this.logger = new Mock<ILogger>();

            this.target = new DeviceModelsController(
                this.deviceModelsService.Object,
                this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task ItReturnsTheListOfDeviceModels()
        {
            // Arrange
            var deviceModels = this.GetDeviceModels();
            
            this.deviceModelsService
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(deviceModels);

            // Act
            var result = await this.target.GetAsync();

            // Assert
            Assert.Equal(deviceModels.Count, result.Items.Count);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async Task ItReturnsTheDeviceModelById()
        {
            // Arrange
            const string id = "deviceModelId";
            var deviceModel = this.GetDeviceModelById(id);

            this.deviceModelsService
                .Setup(x => x.GetAsync(id))
                .ReturnsAsync(deviceModel);

            // Act
            var result = await this.target.GetAsync(id);

            // Assert
            Assert.Equal(deviceModel.Id, result.Id);
        }

        private List<DeviceModel> GetDeviceModels()
        {
            var deviceModels = new List<DeviceModel>
            {
                new DeviceModel { Id = "Id_1", ETag = "Etag_1" },
                new DeviceModel { Id = "Id_2", ETag = "Etag_2" },
                new DeviceModel { Id = "Id_3", ETag = "Etag_3" },
                new DeviceModel { Id = "Id_4", ETag = "Etag_4" },
                new DeviceModel { Id = "Id_5", ETag = "Etag_5" }
            };

            return deviceModels;
        }

        private DeviceModel GetDeviceModelById(string id)
        {
            var deviceModel = new DeviceModel
            {
                Id = id,
                ETag = "Etag_1"
            };

            return deviceModel;
        }
    }
}
