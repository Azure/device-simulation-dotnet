// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationScriptApiModel;
using Moq;
using Newtonsoft.Json.Linq;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Controllers
{
    public class SimulationScriptsControllerTest
    {
        private readonly Mock<ISimulationScripts> simulationScriptsService;
        private readonly Mock<ILogger> logger;
        private readonly SimulationScriptsController target;

        public SimulationScriptsControllerTest()
        {
            this.simulationScriptsService = new Mock<ISimulationScripts>();
            this.logger = new Mock<ILogger>();

            this.target = new SimulationScriptsController(
                this.simulationScriptsService.Object,
                this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetReturnsTheListOfSimulationScripts()
        {
            // Arrange
            var simulationScripts = this.GetSimulationScripts();

            this.simulationScriptsService
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(simulationScripts);

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal(simulationScripts.Count, result.Items.Count);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetReturnsTheSimulationScriptById()
        {
            // Arrange
            const string ID = "simulationScriptId";
            var simulationScript = this.GetSimulationScriptById(ID);

            this.simulationScriptsService
                .Setup(x => x.GetAsync(ID))
                .ReturnsAsync(simulationScript);

            // Act
            var result = this.target.GetAsync(ID).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(simulationScript.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetReturnsNullWithInvalidId()
        {
            // Arrange
            const string ID = "NoneExistedId";

            // Act
            var result = this.target.GetAsync(ID).Result;

            // Assert
            Assert.Null(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void PostCreatesTheSimulationScript()
        {
            // Arrange
            const string ID = "simulationScriptId";
            var simulationScript = this.GetSimulationScriptById(ID);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("some scripts"));
            var file = new FormFile(stream, 10, 10, "test", "test file");

            this.simulationScriptsService
                .Setup(x => x.InsertAsync(It.IsAny<SimulationScript>()))
                .ReturnsAsync(simulationScript);

            // Act
            var result = this.target.PostAsync(file).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(simulationScript.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void PostThrowsErrorWithInvalidSimulationScript()
        {
            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await this.target.PostAsync(null))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void DeleteInvokesSimulationScriptServiceWithId()
        {
            // Arrange
            const string ID = "simulationScriptId";

            // Act
            this.target.DeleteAsync(ID)
                .Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.simulationScriptsService.Verify(x => x.DeleteAsync(ID), Times.Once);
        }

        private SimulationScript GetSimulationScriptById(string id)
        {
            return new SimulationScript
            {
                Id = id,
                ETag = "etag",
                Path = SimulationScript.SimulationScriptPath.Storage
            };
        }

        private List<SimulationScript> GetSimulationScripts()
        {
            return new List<SimulationScript>
            {
                new SimulationScript { Id = "Id_1", ETag = "Etag_1" },
                new SimulationScript { Id = "Id_2", ETag = "Etag_2" },
                new SimulationScript { Id = "Id_3", ETag = "Etag_3" }
            };
        }
    }
}
