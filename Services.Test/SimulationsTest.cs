// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Moq;
using Newtonsoft.Json;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;
using SimulationModel = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;

namespace Services.Test
{
    public class SimulationsTest
    {
        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper log;

        private const string StorageCollection = "simulations";
        private const string SimulationId = "1";

        private readonly Mock<IDeviceTypes> deviceTypes;
        private readonly Mock<IStorageAdapterClient> storage;
        private readonly Mock<ILogger> logger;
        private readonly Simulations target;
        private readonly List<DeviceType> types;

        public SimulationsTest(ITestOutputHelper log)
        {
            this.log = log;

            this.deviceTypes = new Mock<IDeviceTypes>();
            this.storage = new Mock<IStorageAdapterClient>();
            this.logger = new Mock<ILogger>();

            this.types = new List<DeviceType>
            {
                new DeviceType { Id = "01" },
                new DeviceType { Id = "05" },
                new DeviceType { Id = "02" },
                new DeviceType { Id = "AA" }
            };

            this.target = new Simulations(this.deviceTypes.Object, this.storage.Object, this.logger.Object);
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void InitialListIsEmpty()
        {
            // Arrange
            this.ThereAreNoSimulationsInTheStorage();

            // Act
            var list = this.target.GetListAsync().Result;

            // Assert
            Assert.Equal(0, list.Count);
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void InitialMetadataAfterCreation()
        {
            // Arrange
            this.ThereAreNoSimulationsInTheStorage();
            this.ThereAreSomeDeviceTypes();

            // Act
            SimulationModel result = this.target.InsertAsync(new SimulationModel(), "default").Result;

            // Assert
            Assert.Equal(1, result.Version);
            Assert.Equal(result.Created, result.Modified);
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void CreateDefaultSimulation()
        {
            // Arrange
            const int defaultDeviceCount = 2;
            this.ThereAreNoSimulationsInTheStorage();
            this.ThereAreSomeDeviceTypes();

            // Act
            SimulationModel result = this.target.InsertAsync(new SimulationModel(), "default").Result;

            // Assert
            Assert.Equal(this.types.Count, result.DeviceTypes.Count);
            for (var i = 0; i < this.types.Count; i++)
            {
                Assert.Equal(this.types[i].Id, result.DeviceTypes[i].Id);
                Assert.Equal(defaultDeviceCount, result.DeviceTypes[i].Count);
            }
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void CreateSimulationWithoutId()
        {
            // Arrange
            this.ThereAreNoSimulationsInTheStorage();
            this.ThereAreSomeDeviceTypes();

            // Act
            SimulationModel result = this.target.InsertAsync(new SimulationModel(), "default").Result;

            // Assert
            Assert.NotEmpty(result.Id);
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void CreateSimulationWithId()
        {
            // Arrange
            this.ThereAreSomeDeviceTypes();
            this.ThereAreNoSimulationsInTheStorage();

            // Act
            var simulation = new SimulationModel { Id = "123" };
            SimulationModel result = this.target.InsertAsync(simulation, "default").Result;

            // Assert
            Assert.Equal(simulation.Id, result.Id);
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void CreateWithInvalidTemplate()
        {
            // Act + Assert
            Assert.ThrowsAsync<InvalidInputException>(
                () => this.target.InsertAsync(new SimulationModel(), "mytemplate"));
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void CreatingMultipleSimulationsIsNotAllowed()
        {
            // Arrange
            this.ThereAreSomeDeviceTypes();
            this.ThereIsAnEnabledSimulationInTheStorage();

            // Act + Assert
            var s = new SimulationModel { Id = Guid.NewGuid().ToString(), Enabled = false };
            Assert.ThrowsAsync<ConflictingResourceException>(() => this.target.InsertAsync(s));
            Assert.ThrowsAsync<ConflictingResourceException>(() => this.target.UpsertAsync(s));
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void CreatedSimulationsAreStored()
        {
            // Arrange
            this.ThereAreSomeDeviceTypes();
            this.ThereAreNoSimulationsInTheStorage();

            // Act
            var simulation = new SimulationModel { Id = Guid.NewGuid().ToString(), Enabled = false };
            this.target.InsertAsync(simulation, "default").Wait();

            // Assert
            this.storage.Verify(
                x => x.UpdateAsync(StorageCollection, SimulationId, It.IsAny<string>(), "*"));
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void SimulationsCanBeUpserted()
        {
            // Arrange
            this.ThereAreSomeDeviceTypes();
            this.ThereAreNoSimulationsInTheStorage();

            // Act
            var simulation = new SimulationModel
            {
                Id = SimulationId,
                Enabled = false,
                Etag = "2345213461"
            };
            this.target.UpsertAsync(simulation).Wait();

            // Assert
            this.storage.Verify(
                x => x.UpdateAsync(StorageCollection, SimulationId, It.IsAny<string>(), simulation.Etag));
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void UpsertRequiresIdWhileInsertDoesNot()
        {
            // Act
            var s1 = new SimulationModel();
            var s2 = new SimulationModel();
            this.target.InsertAsync(s1);

            // Act + Assert
            Assert.ThrowsAsync<InvalidInputException>(() => this.target.UpsertAsync(s2));
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void UpsertUsesOptimisticConcurrency()
        {
            // Arrange
            this.deviceTypes.Setup(x => x.GetList()).Returns(this.types);

            var id = Guid.NewGuid().ToString();
            var s1 = new SimulationModel { Id = id, Enabled = false };
            this.target.UpsertAsync(s1);

            // Act + Assert
            var s1updated = new SimulationModel { Id = id, Enabled = true };
            Assert.ThrowsAsync<ResourceOutOfDateException>(() => this.target.UpsertAsync(s1updated));
        }

        private void ThereAreSomeDeviceTypes()
        {
            this.deviceTypes.Setup(x => x.GetList()).Returns(this.types);
        }

        private void ThereAreNoSimulationsInTheStorage()
        {
            this.storage.Setup(x => x.GetAllAsync(StorageCollection)).ReturnsAsync(new ValueListApiModel());
        }

        private void ThereIsAnEnabledSimulationInTheStorage()
        {
            var simulation = new SimulationModel
            {
                Id = SimulationId,
                Created = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                Modified = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)),
                Etag = "etag0",
                Enabled = true,
                Version = 1
            };

            var list = new ValueListApiModel();
            var value = new ValueApiModel
            {
                Key = SimulationId,
                Data = JsonConvert.SerializeObject(simulation),
                ETag = simulation.Etag
            };
            list.Items.Add(value);

            this.storage.Setup(x => x.GetAllAsync(StorageCollection)).ReturnsAsync(list);
        }
    }
}
