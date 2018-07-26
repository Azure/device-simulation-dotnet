// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Moq;
using Newtonsoft.Json;
using Services.Test.helpers;
using Xunit;

namespace Services.Test
{
    public class SimulationScriptsTest
    {
        private const string STORAGE_COLLECTION = "simulationScripts";
        private readonly Mock<IStorageAdapterClient> storage;
        private readonly Mock<ILogger> logger;
        private readonly SimulationScripts target;

        public SimulationScriptsTest()
        {
            this.storage = new Mock<IStorageAdapterClient>();
            this.logger = new Mock<ILogger>();

            this.target = new SimulationScripts(
                this.storage.Object,
                this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void InitialListIsEmpty()
        {
            // Arrange
            this.ThereAreNoSimulationScriptsInStorage();

            // Act
            var result = this.target.GetListAsync().Result;

            // Assert
            Assert.Empty(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesSimulationScriptInStorage()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var eTag = Guid.NewGuid().ToString();
            var simulationScript = new SimulationScript { Id = id, ETag = eTag };

            this.storage
                .Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(this.BuildValueApiModel(simulationScript));

            // Act
            SimulationScript result = this.target.InsertAsync(simulationScript).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(simulationScript.Id, result.Id);
            Assert.Equal(simulationScript.ETag, result.ETag);

            this.storage.Verify(
                x => x.UpdateAsync(STORAGE_COLLECTION, simulationScript.Id, It.IsAny<string>(), null), Times.Once());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void SimulationScriptsCanBeUpserted()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();

            var simulationScript = new SimulationScript { Id = id, ETag = "oldEtag" };
            this.TheScriptExists(id, simulationScript);

            var updatedSimulationScript = new SimulationScript { Id = id, ETag = "newETag" };
            this.storage
                .Setup(x => x.UpdateAsync(
                    STORAGE_COLLECTION,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(this.BuildValueApiModel(updatedSimulationScript));

            // Act
            this.target.UpsertAsync(simulationScript)
                .Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.storage.Verify(x => x.GetAsync(STORAGE_COLLECTION, id), Times.Once);
            this.storage.Verify(x => x.UpdateAsync(
                STORAGE_COLLECTION,
                id,
                It.Is<string>(json => JsonConvert.DeserializeObject<SimulationScript>(json).Id == id && !json.Contains("ETag")),
                "oldEtag"), Times.Once());

            Assert.Equal(updatedSimulationScript.Id, simulationScript.Id);
            // The call to UpsertAsync() modifies the object, so the ETags will match
            Assert.Equal(updatedSimulationScript.ETag, simulationScript.ETag);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesSimulationScriptWhenSimulationScriptNotFoundInUpserting()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var simulationScript = new SimulationScript { Id = id, ETag = "Etag" };
            this.TheScriptDoesntExist(id);
            this.storage
                .Setup(x => x.UpdateAsync(
                    STORAGE_COLLECTION,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(this.BuildValueApiModel(simulationScript));

            // Act
            this.target.UpsertAsync(simulationScript).Wait(TimeSpan.FromSeconds(30));

            // Assert - the app uses PUT with given ID
            this.storage.Verify(x => x.UpdateAsync(
                STORAGE_COLLECTION,
                id,
                It.IsAny<string>(),
                null));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsConflictingResourceExceptionIfEtagDoesNotMatchInUpserting()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var simulationScriptInStorage = new SimulationScript { Id = id, ETag = "ETag" };
            this.TheScriptExists(id, simulationScriptInStorage);

            // Act & Assert
            var simulationScript = new SimulationScript { Id = id, ETag = "not-matching-Etag" };
            Assert.ThrowsAsync<ConflictingResourceException>(
                    async () => await this.target.UpsertAsync(simulationScript))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenInsertSimulationScriptFailed()
        {
            // Arrange
            var simulationScript = new SimulationScript { Id = "id", ETag = "Etag" };
            this.storage
                .Setup(x => x.UpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.InsertAsync(simulationScript))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItFailsToUpsertWhenUnableToFetchScriptFromStorage()
        {
            // Arrange
            var simulationScript = new SimulationScript { Id = "id", ETag = "Etag" };
            this.storage
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.UpsertAsync(simulationScript))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItFailsToUpsertWhenStorageUpdateFails()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var simulationScript = new SimulationScript { Id = id, ETag = "Etag" };
            this.TheScriptExists(id, simulationScript);

            this.storage
                .Setup(x => x.UpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.UpsertAsync(simulationScript))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExternalDependencyExceptionWhenFailedFetchingSimulationScriptInStorage()
        {
            // Arrange
            var simulationScript = new SimulationScript { Id = "id", ETag = "Etag" };

            // Act
            var ex = Record.Exception(() => this.target.UpsertAsync(simulationScript).Result);

            // Assert
            Assert.IsType<ExternalDependencyException>(ex.InnerException);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenDeleteSimulationScriptFailed()
        {
            // Arrange
            this.storage
                .Setup(x => x.DeleteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.DeleteAsync("someId"))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItFailsToGetSimulationScriptsWhenStorageFails()
        {
            // Arrange
            this.storage
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new SomeException());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.GetAsync("someId"))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenGetListOfSimulationScriptsDeserializeFailed()
        {
            // Arrange
            this.SetupAListOfInvalidSimulationScriptsInStorage();

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.GetListAsync())
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptionWhenGetSimulationScriptByInvalidId()
        {
            // Act & Assert
            Assert.ThrowsAsync<InvalidInputException>(
                    async () => await this.target.GetAsync(string.Empty))
                .Wait(Constants.TEST_TIMEOUT);
        }

        private void SetupAListOfInvalidSimulationScriptsInStorage()
        {
            var list = new ValueListApiModel();
            var value = new ValueApiModel
            {
                Key = "key",
                Data = "{ 'invalid': json",
                ETag = "etag"
            };
            list.Items.Add(value);

            this.storage
                .Setup(x => x.GetAllAsync(It.IsAny<string>()))
                .ReturnsAsync(list);
        }

        private void TheScriptDoesntExist(string id)
        {
            this.storage
                .Setup(x => x.GetAsync(STORAGE_COLLECTION, id))
                .Throws<ResourceNotFoundException>();
        }

        private void TheScriptExists(string id, SimulationScript simulationScript)
        {
            this.storage
                .Setup(x => x.GetAsync(STORAGE_COLLECTION, id))
                .ReturnsAsync(this.BuildValueApiModel(simulationScript));
        }

        private ValueApiModel BuildValueApiModel(SimulationScript simulationScript)
        {
            return new ValueApiModel
            {
                Key = simulationScript.Id,
                Data = JsonConvert.SerializeObject(simulationScript),
                ETag = simulationScript.ETag
            };
        }

        private void ThereAreNoSimulationScriptsInStorage()
        {
            this.storage
                .Setup(x => x.GetAllAsync(STORAGE_COLLECTION))
                .ReturnsAsync(new ValueListApiModel());
        }
    }
}
