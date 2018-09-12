// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.Clustering
{
    public class ClusterNodesTest
    {
        private readonly ClusterNodes target;
        private readonly Mock<ILogger> log;

        public ClusterNodesTest()
        {
            this.log = new Mock<ILogger>();
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

        private ClusterNodes GetNewInstance()
        {
            return new ClusterNodes(this.log.Object);
        }
    }
}
