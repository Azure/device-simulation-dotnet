// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers;
using Moq;
using WebService.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace WebService.Test.v1.Controllers
{
    public class StatusControllerTest
    {
        private readonly Mock<IStatusService> statusService;
        private readonly Mock<IConfig> config;

        private readonly StatusController target;

        public StatusControllerTest(ITestOutputHelper log)
        {
            this.statusService = new Mock<IStatusService>();
            this.config = new Mock<IConfig>();

            this.target = new StatusController(this.config.Object, this.statusService.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsHealthOfSimulationService()
        {
            //Arrange
            this.statusService
                .Setup(x => x.GetStatusAsync())
                .ReturnsAsync(new StatusServiceModel(true, "Alive and well!"));

            // Act
            var result = this.target.GetAsync().CompleteOrTimeout().Result;

            // Assert
            Assert.True(result.Status.IsHealthy);
        }
    }
}
