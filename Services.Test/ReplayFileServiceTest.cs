// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.CosmosDbSql;
using Moq;
using Newtonsoft.Json;
using Services.Test.helpers;
using System;
using Xunit;

namespace Services.Test
{
    public class ReplayFileServiceTest
    {
        private const string REPLAY_FILES = "replayFiles";
        
        private readonly Mock<ILogger> log;
        private readonly Mock<IServicesConfig> config;
        private readonly Mock<IEngines> enginesFactory;
        private readonly Mock<IEngine> replayFilesStorage;
        private readonly Mock<ILogger> logger;
        private readonly ReplayFileService target;

        public ReplayFileServiceTest()
        {
            this.log = new Mock<ILogger>();
            this.config = new Mock<IServicesConfig>();
            this.enginesFactory = new Mock<IEngines>();

            this.replayFilesStorage = new Mock<IEngine>();
            this.replayFilesStorage.Setup(x => x.BuildRecord(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string id, string json) => new DataRecord { Id = id, Data = json });
            this.replayFilesStorage.Setup(x => x.BuildRecord(It.IsAny<string>()))
                .Returns((string id) => new DataRecord { Id = id });

            this.config.SetupGet(x => x.ReplayFilesStorage)
                .Returns(new Config { CosmosDbSqlCollection = REPLAY_FILES });

            this.enginesFactory
              .Setup(x => x.Build(It.Is<Config>(c => c.CosmosDbSqlCollection == REPLAY_FILES)))
              .Returns(this.replayFilesStorage.Object);

            this.target = new ReplayFileService(
                this.config.Object,
                this.enginesFactory.Object,
                this.log.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesReplayFileInStorage()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var replayFile = new DataFile { Id = id, Content = "1, 2, 3", ETag = "tag" };

            this.replayFilesStorage
                .Setup(x => x.CreateAsync(It.IsAny<IDataRecord>()))
                .ReturnsAsync(new DataRecord { Id = id, Data = JsonConvert.SerializeObject(replayFile) });

            // Act
            DataFile result = this.target.InsertAsync(replayFile).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(replayFile.Id, result.Id);
            Assert.Equal(replayFile.Content, result.Content);

            this.replayFilesStorage.Verify(
                x => x.CreateAsync(It.Is<IDataRecord>(n => n.GetId() == id)), Times.Once());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenCreateReplayFileFails()
        {
            // Arrange
            DataFile replayFile = new DataFile();
            replayFile.Content = "1, 2, 3, 4, 5";

            this.replayFilesStorage
                .Setup(x => x.CreateAsync(It.IsAny<IDataRecord>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.InsertAsync(replayFile))
                .Wait(Constants.TEST_TIMEOUT);
        }


        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenDeleteReplayFileFailes()
        {
            // Arrange
            this.replayFilesStorage
                .Setup(x => x.DeleteAsync(It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.DeleteAsync("Id"))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItFailsToGetReplayFileWhenGetAsyncFails()
        {
            // Arrange
            this.replayFilesStorage
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.GetAsync("Id"))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionForInvalidId()
        {
            // Act & Assert
            Assert.ThrowsAsync<InvalidInputException>(
                    async () => await this.target.GetAsync(string.Empty))
                .Wait(Constants.TEST_TIMEOUT);
        }
    }
}
