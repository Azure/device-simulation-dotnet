// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers;
using Moq;
using Services.Test.helpers;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Controllers
{
    public class DeviceModelPropertiesControllerTest
    {
        private readonly Mock<IDeviceModels> deviceModelsService;
        private readonly DeviceModelPropertiesController target;

        public DeviceModelPropertiesControllerTest()
        {
            this.deviceModelsService = new Mock<IDeviceModels>();
            this.target = new DeviceModelPropertiesController(this.deviceModelsService.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheListOfPropertyNames()
        {
            // Arrange
            var properties = new List<DeviceProperty>
            {
                new DeviceProperty { Id = "Type" },
                new DeviceProperty { Id = "Firmware" },
                new DeviceProperty { Id = "Location" },
                new DeviceProperty { Id = "Model" },
                new DeviceProperty { Id = "Latitude" },
                new DeviceProperty { Id = "Longitude" }
            };

            this.deviceModelsService
                .Setup(x => x.GetPropertyNamesAsync())
                .ReturnsAsync(properties);

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal(properties.Count, result.Items.Count);
            foreach(var prop in result.Items)
            {
                Assert.True(properties.Exists(x => x.Id == prop.Id));
            }
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenGetPropertyNamesFailed()
        {
            // Arrange
            this.deviceModelsService
                .Setup(x => x.GetPropertyNamesAsync())
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<SomeException>(
                    async () => await this.target.GetAsync())
                .Wait(Constants.TEST_TIMEOUT);
        }
    }
}
