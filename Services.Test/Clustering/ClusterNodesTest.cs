// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.Clustering
{
    public class ClusterNodesTest
    {
        private const string NODES = "nodes";
        private const string MAIN = "main";

        private readonly ClusterNodes target;
        private readonly Mock<ILogger> log;
        private readonly Mock<IServicesConfig> config;
        private readonly Mock<IClusteringConfig> clusteringConfig;
        private readonly Mock<IFactory> factory;
        private readonly Mock<IStorageRecords> clusterNodesStorage;
        private readonly Mock<IStorageRecords> mainStorage;

        public ClusterNodesTest()
        {
            this.log = new Mock<ILogger>();
            this.config = new Mock<IServicesConfig>();
            this.clusteringConfig = new Mock<IClusteringConfig>();
            this.factory = new Mock<IFactory>();
            this.clusterNodesStorage = new Mock<IStorageRecords>();
            this.mainStorage = new Mock<IStorageRecords>();

            this.clusteringConfig.SetupGet(x => x.NodeRecordMaxAgeSecs).Returns(12045);

            this.SetupStorageMocks();

            this.target = this.GetNewInstance();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsANodeId()
        {
            // Act
            var id = this.target.GetCurrentNodeId();

            // Assert
            Assert.NotEmpty(id);
            Assert.True(id.Length > 10);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItAlwaysReturnsTheSameNodeId()
        {
            // Arrange
            var instance1 = this.GetNewInstance();
            var instance2 = this.GetNewInstance();

            // Act
            var id1 = instance1.GetCurrentNodeId();
            var id2 = instance2.GetCurrentNodeId();

            // Assert
            Assert.Equal(id1, id2);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItKeepsTheNodeAlive()
        {
            // Arrange
            var nodeId = this.target.GetCurrentNodeId();
            //var instance = this.GetNewInstance();
            this.clusterNodesStorage.Setup(x => x.GetAsync(nodeId)).ReturnsAsync(new StorageRecord { Id = nodeId });

            // Act
            this.target.KeepAliveNodeAsync().CompleteOrTimeout();

            // Assert
            this.clusterNodesStorage.Verify(x => x.GetAsync(nodeId), Times.Once);
            this.clusterNodesStorage.Verify(x => x.UpsertAsync(It.Is<StorageRecord>(n => n.Id == nodeId && !n.IsExpired())), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesNodesWhenMissing()
        {
            // Arrange
            var nodeId = this.target.GetCurrentNodeId();
            this.clusterNodesStorage.Setup(x => x.GetAsync(nodeId)).Throws<ResourceNotFoundException>();

            // Act
            this.target.KeepAliveNodeAsync().CompleteOrTimeout();

            // Assert
            this.clusterNodesStorage.Verify(x => x.GetAsync(nodeId), Times.Once);
            this.clusterNodesStorage.Verify(x => x.UpsertAsync(It.IsAny<StorageRecord>()), Times.Never);
            this.clusterNodesStorage.Verify(x => x.CreateAsync(It.Is<StorageRecord>(n => n.Id == nodeId && !n.IsExpired())), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesTheMasterRecordWhenMissing()
        {
            // Arrange
            this.clusteringConfig.SetupGet(x => x.MasterLockDurationSecs).Returns(123);
            this.mainStorage.Setup(x => x.ExistsAsync(ClusterNodes.MASTER_NODE_KEY)).ReturnsAsync(false);

            // Act
            var instance = this.GetNewInstance();
            instance.SelfElectToMasterNodeAsync().CompleteOrTimeout();

            // Assert
            this.mainStorage
                .Verify(x => x.CreateAsync(It.Is<StorageRecord>(r => r.Id == ClusterNodes.MASTER_NODE_KEY)), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItAllowsOnlyOneNodeToBecomeMaster()
        {
            // Arrange
            var tryToLockResult = new Queue<bool>();
            tryToLockResult.Enqueue(true);
            tryToLockResult.Enqueue(false);
            tryToLockResult.Enqueue(false);
            tryToLockResult.Enqueue(true);
            this.mainStorage.Setup(x => x.TryToLockAsync(
                    ClusterNodes.MASTER_NODE_KEY,
                    this.target.GetCurrentNodeId(),
                    It.IsAny<string>(),
                    It.IsAny<int>()))
                .ReturnsAsync(tryToLockResult.Dequeue);

            // Act - Run multiple calls to ensure the state comes from the storage
            var result1 = this.target.SelfElectToMasterNodeAsync().CompleteOrTimeout().Result;
            var result2 = this.target.SelfElectToMasterNodeAsync().CompleteOrTimeout().Result;
            var result3 = this.target.SelfElectToMasterNodeAsync().CompleteOrTimeout().Result;
            var result4 = this.target.SelfElectToMasterNodeAsync().CompleteOrTimeout().Result;

            // Assert
            this.mainStorage
                .Verify(x => x.TryToLockAsync(
                    ClusterNodes.MASTER_NODE_KEY,
                    this.target.GetCurrentNodeId(),
                    It.IsAny<string>(),
                    It.IsAny<int>()), Times.Exactly(4));
            Assert.True(result1);
            Assert.False(result2);
            Assert.False(result3);
            Assert.True(result4);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntAllowMasterElectionInCaseOfErrors()
        {
            // Arrange
            this.mainStorage.Setup(x => x.TryToLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Throws<SomeException>();

            // Act
            var result = this.target.SelfElectToMasterNodeAsync().CompleteOrTimeout().Result;

            // Assert
            Assert.False(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntAllowMasterElectionInCaseOfConflict()
        {
            // Arrange
            this.mainStorage.Setup(x => x.TryToLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Throws<ConflictingResourceException>();

            // Act
            var result = this.target.SelfElectToMasterNodeAsync().CompleteOrTimeout().Result;

            // Assert
            Assert.False(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItRemovesStaleNodes()
        {
            // Act
            this.target.RemoveStaleNodesAsync().CompleteOrTimeout();

            // Assert
            this.clusterNodesStorage.Verify(x => x.GetAllAsync(), Times.Once);
        }

        private ClusterNodes GetNewInstance()
        {
            return new ClusterNodes(
                this.config.Object,
                this.clusteringConfig.Object,
                this.factory.Object,
                this.log.Object);
        }

        // Setup the storage mocks to return the right mock depending on the collection name.
        // This is needed because IStorageRecords is used multiple times, once per collection.
        private void SetupStorageMocks()
        {
            // Inject configuration settings with a collection name which is then used
            // to intercept the call to .InitAsync()
            this.config.SetupGet(x => x.NodesStorage)
                .Returns(new StorageConfig { DocumentDbCollection = NODES });
            this.config.SetupGet(x => x.MainStorage)
                .Returns(new StorageConfig { DocumentDbCollection = MAIN });

            // Intercept the call to .InitAsync() and return the right mock depending on the collection name
            var storageMockFactory = new Mock<IStorageRecords>();
            storageMockFactory
                .Setup(x => x.Init(It.Is<StorageConfig>(c => c.DocumentDbCollection == MAIN)))
                .Returns(this.mainStorage.Object);
            storageMockFactory
                .Setup(x => x.Init(It.Is<StorageConfig>(c => c.DocumentDbCollection == NODES)))
                .Returns(this.clusterNodesStorage.Object);

            // When IStorageRecords is instantiated, return the factory above
            this.factory.Setup(x => x.Resolve<IStorageRecords>()).Returns(storageMockFactory.Object);
        }
    }
}
