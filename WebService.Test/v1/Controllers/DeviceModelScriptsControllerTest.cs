// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Moq;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Controllers
{
    public class DeviceModelScriptsControllerTest
    {
        private readonly Mock<IDeviceModelScripts> deviceModelScriptsService;
        private readonly Mock<IJavascriptInterpreter> javascriptInterpreter;
        private readonly Mock<ILogger> logger;
        private readonly DeviceModelScriptsController target;

        public DeviceModelScriptsControllerTest()
        {
            this.deviceModelScriptsService = new Mock<IDeviceModelScripts>();
            this.javascriptInterpreter = new Mock<IJavascriptInterpreter>();
            this.logger = new Mock<ILogger>();

            this.target = new DeviceModelScriptsController(
                this.deviceModelScriptsService.Object,
                this.javascriptInterpreter.Object,
                this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetReturnsTheListOfDeviceModelScripts()
        {
            // Arrange
            var deviceModelScripts = this.GetDeviceModelScripts();

            this.deviceModelScriptsService
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(deviceModelScripts);

            // Act
            var result = this.target.GetAsync().Result;

            // Assert
            Assert.Equal(deviceModelScripts.Count, result.Items.Count);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetReturnsTheDeviceModelScriptById()
        {
            // Arrange
            const string ID = "deviceModelScriptId";
            var deviceModelScript = this.GetDeviceModelScriptById(ID);

            this.deviceModelScriptsService
                .Setup(x => x.GetAsync(ID))
                .ReturnsAsync(deviceModelScript);

            // Act
            var result = this.target.GetAsync(ID).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModelScript.Id, result.Id);
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
        public void PostCreatesTheDeviceModelScript()
        {
            // Arrange
            const string ID = "deviceModelScriptId";
            var deviceModelScript = this.GetDeviceModelScriptById(ID);
            IFormFile file = this.SetupFileMock();

            this.deviceModelScriptsService
                .Setup(x => x.InsertAsync(It.IsAny<DataFile>()))
                .ReturnsAsync(deviceModelScript);

            // Act
            var result = this.target.PostAsync(file).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceModelScript.Id, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void PostThrowsErrorWithInvalidDeviceModelScript()
        {
            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await this.target.PostAsync(null))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void PutUpdatesTheDeviceModelScript()
        {
            // Arrange
            const string ID = "deviceModelScriptId";
            var deviceModelScript = this.GetDeviceModelScriptById(ID);
            IFormFile file = this.SetupFileMock();

            this.deviceModelScriptsService
                .Setup(x => x.UpsertAsync(It.IsAny<DataFile>()))
                .ReturnsAsync(deviceModelScript);

            // Act
            var result = this.target.PutAsync(file, deviceModelScript.ETag, ID).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ID, result.Id);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void PutThrowsErrorWithInvalidDeviceModelScript()
        {
            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(
                    async () => await this.target.PutAsync(null, null, null))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void DeleteInvokesDeviceModelScriptServiceWithId()
        {
            // Arrange
            const string ID = "deviceModelScriptId";

            // Act
            this.target.DeleteAsync(ID)
                .Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.deviceModelScriptsService.Verify(x => x.DeleteAsync(ID), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItValidatesTheDeviceModelScript()
        {
            // Arrange
            IFormFile file = this.SetupFileMock();

            // Act
            var result = this.target.Validate(file);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<JsonResult>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsErrorWhenValidationFails()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            const string FILE_NAME = "test.docx";

            fileMock.Setup(x => x.FileName).Returns(FILE_NAME);

            // Act & Assert
            Assert.Throws<BadRequestException>(() =>  this.target.Validate(fileMock.Object));
        }

        private IFormFile SetupFileMock()
        {
            var fileMock = new Mock<IFormFile>();
            const string CONTENT_TYPE = "text/javascript";
            const string CONTENT = "awesome javascript";
            const string FILENAME = "test.js";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(CONTENT));

            fileMock.Setup(x => x.ContentType).Returns(CONTENT_TYPE);
            fileMock.Setup(x => x.FileName).Returns(FILENAME);
            fileMock.Setup(x => x.OpenReadStream()).Returns(ms);

            return fileMock.Object;
        }

        private DataFile GetDeviceModelScriptById(string id)
        {
            return new DataFile
            {
                Id = id,
                ETag = "etag",
                Path = DataFile.FilePath.Storage
            };
        }

        private List<DataFile> GetDeviceModelScripts()
        {
            return new List<DataFile>
            {
                new DataFile { Id = "Id_1", ETag = "Etag_1" },
                new DataFile { Id = "Id_2", ETag = "Etag_2" },
                new DataFile { Id = "Id_3", ETag = "Etag_3" }
            };
        }
    }
}
