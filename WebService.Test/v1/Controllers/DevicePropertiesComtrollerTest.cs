// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel;
using Moq;
using Newtonsoft.Json.Linq;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Controllers
{
    public class DevicePropertiesComtrollerTest
    {
        private readonly Mock<IDeviceModels> deviceModelsService;
        private readonly DevicePropertiesController target;

        public DevicePropertiesComtrollerTest()
        {
            this.deviceModelsService = new Mock<IDeviceModels>();
            this.target = new DevicePropertiesController(this.deviceModelsService.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTheListOfPropertyNames()
        {
            // Arrange
            string[] propertiesArray =
            {
                "Type",
                "Firmware",
                "Location",
                "Model",
                "Latitude",
                "Longitude"
            };
            var properties = new HashSet<string>(propertiesArray);

            this.deviceModelsService
                .Setup(x => x.GetPropertyNamesAsync())
                .ReturnsAsync(properties);

            // Act
            var result = this.target.GetDevicePropertiesAsync().Result;

            // Assert
            Assert.True(result.ReportedProperties.Equals(properties));
        }
    }
}
