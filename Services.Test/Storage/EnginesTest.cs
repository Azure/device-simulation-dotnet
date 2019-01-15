// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Moq;
using Services.Test.helpers;
using Xunit;
using CosmosDbSqlEngine = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.CosmosDbSql.Engine;
using TableStorageEngine = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.TableStorage.Engine;

namespace Services.Test.Storage
{
    public class EnginesTest
    {
        private readonly Engines target;

        private readonly Mock<IFactory> factory;
        private readonly Mock<IInstance> instance;

        public EnginesTest()
        {
            this.factory = new Mock<IFactory>();
            this.target = new Engines(this.factory.Object);

            // Mocking takes some extra effort because multiple engines
            // implement the same interface and don't have a parameterless ctor
            this.instance = new Mock<IInstance>();
            var logger = new Mock<ILogger>();
            this.factory.Setup(x => x.Resolve<CosmosDbSqlEngine>())
                .Returns(new CosmosDbSqlEngine(this.factory.Object, logger.Object, this.instance.Object));
            this.factory.Setup(x => x.Resolve<TableStorageEngine>())
                .Returns(new TableStorageEngine(this.factory.Object, logger.Object, this.instance.Object));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItBuildsCosmosDbSqlEngines()
        {
            // Act
            var config = new Config { StorageType = Type.CosmosDbSql };
            var engine = this.target.Build(config);

            // Assert
            Assert.True(engine is CosmosDbSqlEngine);

            // Cannot verify the call to Init because it's not a virtual method, so we
            // assert that indirectly by checking if the engine internally is initialized
            this.instance.Verify(x => x.InitComplete(), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItBuildsTableStorageEngines()
        {
            // Act
            var config = new Config { StorageType = Type.TableStorage };
            var engine = this.target.Build(config);

            // Assert
            Assert.True(engine is TableStorageEngine);

            // Cannot verify the call to Init because it's not a virtual method, so we
            // assert that indirectly by checking if the engine internally is initialized
            this.instance.Verify(x => x.InitComplete(), Times.Once);
        }
    }
}
