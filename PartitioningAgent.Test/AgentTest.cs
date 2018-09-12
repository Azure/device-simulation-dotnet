// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.PartitioningAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Moq;
using PartitioningAgent.Test.helpers;
using Xunit;

namespace PartitioningAgent.Test
{
    public class AgentTest
    {
        private readonly Agent target;
        private readonly Mock<IClusterNodes> clusterNodes;
        private readonly Mock<IThreadWrapper> thread;
        private readonly Mock<IClusteringConfig> clusteringConfig;
        private readonly Mock<ILogger> log;

        public AgentTest()
        {
            this.clusterNodes = new Mock<IClusterNodes>();
            this.thread = new Mock<IThreadWrapper>();
            this.clusteringConfig = new Mock<IClusteringConfig>();
            this.log = new Mock<ILogger>();

            this.clusteringConfig.SetupGet(x => x.CheckIntervalMsecs).Returns(5);
            this.thread.Setup(x => x.Sleep(It.IsAny<int>()))
                .Callback(() => { Thread.Sleep(1); });

            this.target = new Agent(
                this.clusterNodes.Object,
                this.thread.Object,
                this.clusteringConfig.Object,
                this.log.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItKeepsTheNodeAlive()
        {
            // Arrange
            this.clusterNodes.Setup(x => x.KeepAliveNodeAsync())
                .Callback(() => this.target.Stop())
                .Returns(Task.CompletedTask);

            // Act
            var task = this.target.StartAsync();

            // Assert
            this.clusterNodes.Verify(x => x.KeepAliveNodeAsync(), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItStopsWhenAskedTo()
        {
            // Arrange
            var task = Task.Factory.StartNew(() => this.target.StartAsync());
            WaitForTaskStatus(task, TaskStatus.Running, 2500);
            Assert.Equal(TaskStatus.Running, task.Status);

            // Act
            this.target.Stop();
            WaitForTaskStatus(task, TaskStatus.RanToCompletion, Constants.TEST_TIMEOUT);

            // Assert
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }

        // Helper used to ensure that a task reaches an expected state
        private static void WaitForTaskStatus(Task<Task> task, TaskStatus status, int time)
        {
            var pause = 20;
            var count = time / pause;
            while (task.Status != status && count-- > 0)
                Thread.Sleep(pause);
        }
    }
}
