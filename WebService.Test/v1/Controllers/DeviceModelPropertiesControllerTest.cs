// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
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
            var properties = new List<string>
            {
                "Type", "Firmware" , "Location" ,"Model", "Latitude" ,"Longitude"
            };

            this.deviceModelsService
                .Setup(x => x.GetPropertyNamesAsync())
                .ReturnsAsync(properties);

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal(properties.Count, result.Items.Count);
            foreach (var resultItem in result.Items)
            {
                Assert.Contains(resultItem, properties);
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
