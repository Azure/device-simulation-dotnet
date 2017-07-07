// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Moq;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace Services.Test
{
    public class SimulationsTest
    {
        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper log;

        private readonly Mock<IDeviceTypes> deviceTypes;
        private readonly Simulations target;
        private readonly List<DeviceType> types;
        private readonly Mock<ILogger> logger;

        public SimulationsTest(ITestOutputHelper log)
        {
            this.log = log;

            var tempStorage = Guid.NewGuid() + ".json";
            this.log.WriteLine("Temporary simulations storage: " + tempStorage);

            this.deviceTypes = new Mock<IDeviceTypes>();
            this.logger = new Mock<ILogger>();

            this.types = new List<DeviceType>
            {
                new DeviceType { Id = "01" },
                new DeviceType { Id = "05" },
                new DeviceType { Id = "02" },
                new DeviceType { Id = "AA" }
            };

            this.target = new Simulations(this.deviceTypes.Object, this.logger.Object);
            this.target.ChangeStorageFile(tempStorage);
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void InitialListIsEmpty()
        {
            // Act
            IList<Simulation> list = this.target.GetList();

            // Assert
            Assert.Equal(0, list.Count);
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void InitialMetadataAfterCreation()
        {
            // Arrange
            this.deviceTypes.Setup(x => x.GetList()).Returns(this.types);

            // Act
            Simulation result = this.target.Insert(new Simulation(), "default");

            // Assert
            Assert.Equal(1, result.Version);
            Assert.Equal(result.Created, result.Modified);
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void CreateDefaultSimulation()
        {
            // Arrange
            const int defaultDeviceCount = 2;
            this.deviceTypes.Setup(x => x.GetList()).Returns(this.types);

            // Act
            Simulation result = this.target.Insert(new Simulation(), "default");

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
            this.deviceTypes.Setup(x => x.GetList()).Returns(this.types);

            // Act
            Simulation result = this.target.Insert(new Simulation(), "default");

            // Assert
            Assert.NotEmpty(result.Id);
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void CreateSimulationWithId()
        {
            // Arrange
            this.deviceTypes.Setup(x => x.GetList()).Returns(this.types);

            // Act
            var simulation = new Simulation { Id = "123" };
            Simulation result = this.target.Insert(simulation, "default");

            // Assert
            Assert.Equal(simulation.Id, result.Id);
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void CreateWithInvalidTemplate()
        {
            // Act + Assert
            Assert.Throws<InvalidInputException>(() => this.target.Insert(new Simulation(), "foo"));
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void CreatingMultipleSimulationsIsNotAllowed()
        {
            // Arrange
            this.deviceTypes.Setup(x => x.GetList()).Returns(this.types);
            this.target.Insert(new Simulation(), "default");

            // Act + Assert
            var s = new Simulation { Id = Guid.NewGuid().ToString(), Enabled = false };
            Assert.Throws<ConflictingResourceException>(() => this.target.Insert(s));
            Assert.Throws<ConflictingResourceException>(() => this.target.Upsert(s));
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void CreatedSimulationsAreStored()
        {
            // Arrange
            this.deviceTypes.Setup(x => x.GetList()).Returns(this.types);

            // Act
            var simulation = new Simulation { Id = Guid.NewGuid().ToString(), Enabled = false };
            this.target.Insert(simulation, "default");
            var result = this.target.Get(simulation.Id);

            // Assert
            Assert.Equal(simulation.Id, result.Id);
            Assert.Equal(simulation.Enabled, result.Enabled);
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void SimulationsCanBeUpserted()
        {
            // Arrange
            this.deviceTypes.Setup(x => x.GetList()).Returns(this.types);

            // Act
            var simulation = new Simulation { Id = Guid.NewGuid().ToString(), Enabled = false };
            this.target.Upsert(simulation, "default");
            var result = this.target.Get(simulation.Id);

            // Assert
            Assert.Equal(simulation.Id, result.Id);
            Assert.Equal(simulation.Enabled, result.Enabled);
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void UpsertRequiresIdWhileInsertDoesNot()
        {
            // Act
            var s1 = new Simulation();
            var s2 = new Simulation();
            this.target.Insert(s1);

            // Act + Assert
            Assert.Throws<InvalidInputException>(() => this.target.Upsert(s2));
        }

        [Fact, Trait(Constants.Type, Constants.UnitTest)]
        public void UpsertUsesOptimisticConcurrency()
        {
            // Arrange
            this.deviceTypes.Setup(x => x.GetList()).Returns(this.types);

            var id = Guid.NewGuid().ToString();
            var s1 = new Simulation { Id = id, Enabled = false };
            this.target.Upsert(s1);

            // Act + Assert
            var s1updated = new Simulation { Id = id, Enabled = true };
            Assert.Throws<ResourceOutOfDateException>(() => this.target.Upsert(s1updated));
        }
    }
}
