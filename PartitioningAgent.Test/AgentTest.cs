// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.PartitioningAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Moq;
using PartitioningAgent.Test.helpers;
using Xunit;

namespace PartitioningAgent.Test
{
    public class AgentTest
    {
        private readonly Agent target;
        private readonly Mock<IClusterNodes> clusterNodes;
        private readonly Mock<IDevicePartitions> partitions;
        private readonly Mock<ISimulations> simulations;
        private readonly Mock<IThreadWrapper> thread;
        private readonly Mock<IClusteringConfig> clusteringConfig;
        private readonly Mock<ILogger> log;

        public AgentTest()
        {
            this.clusterNodes = new Mock<IClusterNodes>();
            this.partitions = new Mock<IDevicePartitions>();
            this.simulations = new Mock<ISimulations>();
            this.thread = new Mock<IThreadWrapper>();
            this.clusteringConfig = new Mock<IClusteringConfig>();
            this.log = new Mock<ILogger>();

            this.clusteringConfig.SetupGet(x => x.CheckIntervalMsecs).Returns(5);
            this.thread.Setup(x => x.Sleep(It.IsAny<int>()))
                .Callback(() => { Thread.Sleep(1); });

            // Setup empty storage by default
            this.partitions.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<DevicesPartition>());
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation>());

            this.target = new Agent(
                this.clusterNodes.Object,
                this.partitions.Object,
                this.simulations.Object,
                this.thread.Object,
                this.clusteringConfig.Object,
                this.log.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItKeepsTheNodeAlive()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();

            // Act
            this.target.StartAsync().CompleteOrTimeout();

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

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItRemovesStaleNodesOnlyIfItIsAMaster()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();

            // Arrange - Not Master
            this.clusterNodes.Setup(x => x.SelfElectToMasterNodeAsync()).ReturnsAsync(false);

            // Act
            this.target.StartAsync().CompleteOrTimeout();

            // Assert
            this.clusterNodes.Verify(x => x.RemoveStaleNodesAsync(), Times.Never);

            // Arrange - Is Master
            this.clusterNodes.Setup(x => x.SelfElectToMasterNodeAsync()).ReturnsAsync(true);

            // Act
            this.target.StartAsync().CompleteOrTimeout();

            // Assert
            this.clusterNodes.Verify(x => x.RemoveStaleNodesAsync(), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItManagesPartitionsOnlyIfItIsAMaster()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();

            // Arrange - List of simulations to partition
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation>
            {
                new Simulation
                {
                    Id = "1111",
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    PartitioningComplete = false
                }
            });

            // Arrange - List of partitions to delete
            this.partitions.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<DevicesPartition>
            {
                new DevicesPartition { SimulationId = "9999" }
            });

            // #1: Arrange - Not Master
            this.clusterNodes.Setup(x => x.SelfElectToMasterNodeAsync()).ReturnsAsync(false);

            // Act
            this.target.StartAsync().CompleteOrTimeout();

            // Assert
            this.partitions.Verify(x => x.CreateAsync(It.IsAny<string>()), Times.Never);
            this.partitions.Verify(x => x.DeleteListAsync(It.IsAny<List<string>>()), Times.Never);

            // #2: Arrange - Is Master
            this.clusterNodes.Setup(x => x.SelfElectToMasterNodeAsync()).ReturnsAsync(true);

            // Act
            this.target.StartAsync().CompleteOrTimeout();

            // Assert
            this.partitions.Verify(x => x.CreateAsync(It.IsAny<string>()), Times.Once);
            this.partitions.Verify(x => x.DeleteListAsync(It.IsAny<List<string>>()), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItPartitionsOnlySimulationsNotYetPartitioned()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();

            // Arrange - Is Master
            this.clusterNodes.Setup(x => x.SelfElectToMasterNodeAsync()).ReturnsAsync(true);

            // Arrange - List of simulations, one is already partitioned
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation>
            {
                new Simulation
                {
                    Id = "1111",
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    PartitioningComplete = true
                },
                new Simulation
                {
                    Id = "1111",
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    PartitioningComplete = false
                },
                new Simulation
                {
                    Id = "1111",
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    PartitioningComplete = false
                },
            });

            // Act
            this.target.StartAsync().CompleteOrTimeout();

            // Assert
            this.partitions.Verify(x => x.CreateAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDeletesOnlyPartitionsBelongingToInactiveSimulations()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();

            // Arrange - List of simulations to partition
            const string SIM1_ID = "1111";
            const string SIM2_ID = "2222";
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation>
            {
                new Simulation
                {
                    Id = SIM1_ID,
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    PartitioningComplete = false
                },
                new Simulation
                {
                    Id = SIM2_ID,
                    Enabled = false,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    PartitioningComplete = false
                }
            });

            // Arrange - List of partitions, 4 to be deleted because
            // they belong to SIM2_ID which is not active
            this.partitions.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<DevicesPartition>
            {
                new DevicesPartition { Id = SIM1_ID + "_1", SimulationId = SIM1_ID },
                new DevicesPartition { Id = SIM1_ID + "_2", SimulationId = SIM1_ID },
                new DevicesPartition { Id = SIM1_ID + "_3", SimulationId = SIM1_ID },
                new DevicesPartition { Id = SIM2_ID + "_1", SimulationId = SIM2_ID },
                new DevicesPartition { Id = SIM2_ID + "_2", SimulationId = SIM2_ID },
                new DevicesPartition { Id = SIM2_ID + "_3", SimulationId = SIM2_ID },
                new DevicesPartition { Id = SIM2_ID + "_4", SimulationId = SIM2_ID }
            });

            // Arrange - Not Master
            this.clusterNodes.Setup(x => x.SelfElectToMasterNodeAsync()).ReturnsAsync(true);

            // Act
            this.target.StartAsync().CompleteOrTimeout();

            // Assert
            this.partitions.Verify(x => x.DeleteListAsync(It.Is<List<string>>(l => l.Count == 4)), Times.Once);
        }

        // Helper used to ensure that a task reaches an expected state
        private static void WaitForTaskStatus(Task<Task> task, TaskStatus status, int time)
        {
            var pause = 20;
            var count = time / pause;
            while (task.Status != status && count-- > 0)
                Thread.Sleep(pause);
        }

        private void AfterStartRunOnlyOneLoop()
        {
            this.thread.Setup(x => x.Sleep(It.IsAny<int>()))
                .Callback(() => this.target.Stop());
        }
    }
}
