// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.PartitioningAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.AzureManagementAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Moq;
using PartitioningAgent.Test.helpers;
using Xunit;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;

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
        private readonly Mock<IFactory> factory;
        private readonly Mock<ILogger> log;
        private readonly Mock<IDevices> devices;
        private readonly Mock<IAzureManagementAdapterClient> azureManagementAdapterClient;

        public AgentTest()
        {
            this.clusterNodes = new Mock<IClusterNodes>();
            this.partitions = new Mock<IDevicePartitions>();
            this.simulations = new Mock<ISimulations>();
            this.thread = new Mock<IThreadWrapper>();
            this.clusteringConfig = new Mock<IClusteringConfig>();
            this.factory = new Mock<IFactory>();
            this.log = new Mock<ILogger>();
            this.azureManagementAdapterClient = new Mock<IAzureManagementAdapterClient>();

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
                this.factory.Object,
                this.log.Object,
                this.azureManagementAdapterClient.Object);

            this.devices = new Mock<IDevices>();
            this.factory.Setup(x => x.Resolve<IDevices>()).Returns(this.devices.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItKeepsTheNodeAlive()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.clusterNodes.Verify(x => x.KeepAliveNodeAsync(), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItStopsWhenAskedTo()
        {
            // Arrange
            var task = Task.Factory.StartNew(() => this.target.StartAsync(CancellationToken.None));
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
            this.TheCurrentNodeIsNotMaster();

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.clusterNodes.Verify(x => x.RemoveStaleNodesAsync(), Times.Never);

            // Arrange
            this.TheCurrentNodeIsMaster();

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.clusterNodes.Verify(x => x.RemoveStaleNodesAsync(), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesDevicesOnlyIfItIsAMaster()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();

            // Arrange - List of simulations with devices to create
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation>
            {
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    DevicesCreationComplete = false
                }
            });

            // Arrange
            this.TheCurrentNodeIsNotMaster();

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.simulations.Verify(x => x.TryToStartDevicesCreationAsync(It.IsAny<string>(), It.IsAny<IDevices>()), Times.Never);

            // Arrange
            this.TheCurrentNodeIsMaster();

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.simulations.Verify(x => x.TryToStartDevicesCreationAsync(It.IsAny<string>(), It.IsAny<IDevices>()), Times.Once);
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
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    PartitioningComplete = false
                }
            });

            // Arrange - List of partitions to delete
            this.partitions.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<DevicesPartition>
            {
                new DevicesPartition { SimulationId = Guid.NewGuid().ToString() }
            });

            // #1: Arrange
            this.TheCurrentNodeIsNotMaster();

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.partitions.Verify(x => x.CreateAsync(It.IsAny<string>()), Times.Never);
            this.partitions.Verify(x => x.DeleteListAsync(It.IsAny<List<string>>()), Times.Never);

            // #2: Arrange
            this.TheCurrentNodeIsMaster();

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.partitions.Verify(x => x.CreateAsync(It.IsAny<string>()), Times.Once);
            this.partitions.Verify(x => x.DeleteListAsync(It.IsAny<List<string>>()), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItPartitionsOnlySimulationsNotYetPartitioned()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();
            this.TheCurrentNodeIsMaster();

            // Arrange - List of simulations, one is already partitioned
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation>
            {
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    PartitioningComplete = true
                },
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    PartitioningComplete = false
                },
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    PartitioningComplete = false
                },
            });

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.partitions.Verify(x => x.CreateAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDeletesOnlyPartitionsBelongingToInactiveSimulations()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();
            this.TheCurrentNodeIsMaster();

            // Arrange - List of simulations to partition
            string sim1Id = Guid.NewGuid().ToString();
            string sim2Id = Guid.NewGuid().ToString();
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation>
            {
                new Simulation
                {
                    Id = sim1Id,
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    PartitioningComplete = false
                },
                new Simulation
                {
                    Id = sim2Id,
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
                new DevicesPartition { Id = sim1Id + "_1", SimulationId = sim1Id },
                new DevicesPartition { Id = sim1Id + "_2", SimulationId = sim1Id },
                new DevicesPartition { Id = sim1Id + "_3", SimulationId = sim1Id },
                new DevicesPartition { Id = sim2Id + "_1", SimulationId = sim2Id },
                new DevicesPartition { Id = sim2Id + "_2", SimulationId = sim2Id },
                new DevicesPartition { Id = sim2Id + "_3", SimulationId = sim2Id },
                new DevicesPartition { Id = sim2Id + "_4", SimulationId = sim2Id }
            });

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.partitions.Verify(x => x.DeleteListAsync(It.Is<List<string>>(l => l.Count == 4)), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesDevicesOnlyIfNeeded()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();
            this.TheCurrentNodeIsMaster();

            // Arrange - List of simulations with devices to create
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation>
            {
                // Not active
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = false,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    DevicesCreationComplete = false
                },
                // Ran in the past
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                    EndTime = DateTimeOffset.UtcNow.AddHours(-1),
                    DevicesCreationComplete = false
                },
                // Device creation already done
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    DevicesCreationComplete = true
                },
                // Device creation already started
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    DevicesCreationStarted = true,
                    DevicesCreationComplete = false
                }
            });

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.simulations.Verify(x => x.TryToStartDevicesCreationAsync(It.IsAny<string>(), It.IsAny<IDevices>()), Times.Never);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItChecksIfDeviceCreationIsCompleteWhenItAlreadyStarted()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();
            this.TheCurrentNodeIsMaster();

            var jobId = Guid.NewGuid().ToString();
            var deviceService = new Mock<IDevices>();
            deviceService.Setup(x => x.IsJobCompleteAsync(jobId, It.IsAny<Action>())).ReturnsAsync(false);
            this.factory.Setup(x => x.Resolve<IDevices>()).Returns(deviceService.Object);
            var simulation = new Simulation
            {
                Id = Guid.NewGuid().ToString(),
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                DevicesCreationStarted = true,
                DevicesCreationComplete = false,
                DeviceCreationJobId = jobId
            };
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation> { simulation });

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.simulations.Verify(x => x.TryToStartDevicesCreationAsync(It.IsAny<string>(), It.IsAny<IDevices>()), Times.Never);
            deviceService.Verify(x => x.InitAsync(), Times.Once);
            deviceService.Verify(x => x.IsJobCompleteAsync(simulation.DeviceCreationJobId, It.IsAny<Action>()), Times.Once);
            this.simulations.Verify(x => x.TryToSetDeviceCreationCompleteAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItChangesTheSimulationStateWhenTheBulkCreationJobIsComplete()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();
            this.TheCurrentNodeIsMaster();

            var jobId = Guid.NewGuid().ToString();
            var deviceService = new Mock<IDevices>();
            deviceService.Setup(x => x.IsJobCompleteAsync(jobId, It.IsAny<Action>())).ReturnsAsync(true);
            this.factory.Setup(x => x.Resolve<IDevices>()).Returns(deviceService.Object);
            var simulation = new Simulation
            {
                Id = Guid.NewGuid().ToString(),
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                DevicesCreationStarted = true,
                DevicesCreationComplete = false,
                DeviceCreationJobId = jobId
            };
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation> { simulation });

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            deviceService.Verify(x => x.IsJobCompleteAsync(jobId, It.IsAny<Action>()), Times.Once);
            this.simulations.Verify(x => x.TryToSetDeviceCreationCompleteAsync(simulation.Id), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItRetriesTheDeviceCreationIfThePreviousCreationFailed()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();
            this.TheCurrentNodeIsMaster();

            var jobId = Guid.NewGuid().ToString();
            var deviceService = new Mock<IDevices>();
            deviceService.Setup(x => x.IsJobCompleteAsync(jobId, It.IsAny<Action>()))
                .Callback((string job, Action recreateJobSignal) => recreateJobSignal.Invoke())
                .ReturnsAsync(false);
            this.factory.Setup(x => x.Resolve<IDevices>()).Returns(deviceService.Object);
            var simulation = new Simulation
            {
                Id = Guid.NewGuid().ToString(),
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                DevicesCreationComplete = false,
                DevicesCreationStarted = true,
                DeviceCreationJobId = jobId
            };
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation> { simulation });

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            deviceService.Verify(x => x.IsJobCompleteAsync(simulation.DeviceCreationJobId, It.IsAny<Action>()), Times.Once);
            this.simulations.Verify(x => x.TryToSetDeviceCreationCompleteAsync(It.IsAny<string>()), Times.Never);
            this.simulations.Verify(x => x.TryToStartDevicesCreationAsync(simulation.Id, It.IsAny<IDevices>()), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDeletesDevicesOnlyIfNeeded()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();
            this.TheCurrentNodeIsMaster();

            // Arrange - List of simulations with devices to create
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation>
            {
                // Is active
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    DevicesCreationComplete = false,
                    DeleteDevicesWhenSimulationEnds = true
                },
                // Is running
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(1),
                    DevicesCreationComplete = true,
                    DeleteDevicesWhenSimulationEnds = true
                },
                // Device cleanup not required
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    EndTime = DateTimeOffset.UtcNow.AddHours(+1),
                    DevicesCreationComplete = true,
                    DeleteDevicesWhenSimulationEnds = false
                },
                // Device creation not complete
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                    EndTime = DateTimeOffset.UtcNow.AddHours(-1),
                    DevicesCreationStarted = true,
                    DevicesCreationComplete = false,
                    DeleteDevicesWhenSimulationEnds = true
                }
            });

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.simulations.Verify(x => x.TryToStartDevicesDeletionAsync(It.IsAny<string>(), It.IsAny<IDevices>()), Times.Never);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDeletesDevicesOnlyIfItIsAMaster()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();

            // Arrange - List of simulations with devices to create
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation>
            {
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                    EndTime = DateTimeOffset.UtcNow.AddHours(-1),
                    DevicesCreationComplete = true,
                    DeleteDevicesWhenSimulationEnds = true
                }
            });

            // Arrange
            this.TheCurrentNodeIsNotMaster();

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.simulations.Verify(x => x.TryToStartDevicesDeletionAsync(It.IsAny<string>(), It.IsAny<IDevices>()), Times.Never);

            // Arrange
            this.TheCurrentNodeIsMaster();

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.simulations.Verify(x => x.TryToStartDevicesDeletionAsync(It.IsAny<string>(), It.IsAny<IDevices>()), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItChecksIfDeviceDeletionIsCompleteWhenItAlreadyStarted()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();
            this.TheCurrentNodeIsMaster();

            var jobId = Guid.NewGuid().ToString();
            var deviceService = new Mock<IDevices>();
            deviceService.Setup(x => x.IsJobCompleteAsync(jobId, It.IsAny<Action>())).ReturnsAsync(false);
            this.factory.Setup(x => x.Resolve<IDevices>()).Returns(deviceService.Object);
            var simulation = new Simulation
            {
                Id = Guid.NewGuid().ToString(),
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(-1),
                DevicesCreationComplete = true,
                DeleteDevicesWhenSimulationEnds = true,
                DevicesDeletionStarted = true,
                DevicesDeletionComplete = false,
                DeviceCreationJobId = jobId
            };
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation> { simulation });

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            this.simulations.Verify(x => x.TryToStartDevicesDeletionAsync(It.IsAny<string>(), It.IsAny<IDevices>()), Times.Never);
            deviceService.Verify(x => x.InitAsync(), Times.Once);
            deviceService.Verify(x => x.IsJobCompleteAsync(simulation.DeviceDeletionJobId, It.IsAny<Action>()), Times.Once);
            this.simulations.Verify(x => x.TryToSetDeviceDeletionCompleteAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItChangesTheSimulationStateWhenTheBulkDeletionJobIsComplete()
        {
            // Arrange
            this.AfterStartRunOnlyOneLoop();
            this.TheCurrentNodeIsMaster();

            var jobId = Guid.NewGuid().ToString();
            var deviceService = new Mock<IDevices>();
            deviceService.Setup(x => x.IsJobCompleteAsync(jobId, It.IsAny<Action>())).ReturnsAsync(true);
            this.factory.Setup(x => x.Resolve<IDevices>()).Returns(deviceService.Object);
            var simulation = new Simulation
            {
                Id = Guid.NewGuid().ToString(),
                Enabled = true,
                StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                EndTime = DateTimeOffset.UtcNow.AddHours(-1),
                DevicesCreationComplete = true,
                DeleteDevicesWhenSimulationEnds = true,
                DevicesDeletionStarted = true,
                DevicesDeletionComplete = false,
                DeviceDeletionJobId = jobId
            };
            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation> { simulation });

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            deviceService.Verify(x => x.IsJobCompleteAsync(jobId, It.IsAny<Action>()), Times.Once);
            this.simulations.Verify(x => x.TryToSetDeviceDeletionCompleteAsync(simulation.Id), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCalculatesRequiredNodes()
        {
            // Arrange
            int expectedNodeCount = 9;
            this.AfterStartRunOnlyOneLoop();

            var deviceModels = new List<DeviceModelRef>
            {
                new DeviceModelRef { Id = "d1", Count = 50 },
                new DeviceModelRef { Id = "d2", Count = 150 },
                new DeviceModelRef { Id = "d3", Count = 200 },
            };

            var customDevices = new List<CustomDeviceRef>
            {
                new CustomDeviceRef { DeviceId = "1", DeviceModel = new DeviceModelRef { Id = "d1" } },
                new CustomDeviceRef { DeviceId = "2", DeviceModel = new DeviceModelRef { Id = "d1" } },
                new CustomDeviceRef { DeviceId = "3", DeviceModel = new DeviceModelRef { Id = "d2" } },
                new CustomDeviceRef { DeviceId = "4", DeviceModel = new DeviceModelRef { Id = "d3" } },
                new CustomDeviceRef { DeviceId = "5", DeviceModel = new DeviceModelRef { Id = "d3" } }
            };

            this.simulations.Setup(x => x.GetListAsync()).ReturnsAsync(new List<Simulation>
            {
                new Simulation
                {
                    Id = Guid.NewGuid().ToString(),
                    Enabled = true,
                    StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                    EndTime = DateTimeOffset.UtcNow.AddHours(1),
                    DevicesCreationComplete = true,
                    DeleteDevicesWhenSimulationEnds = true,
                    DeviceModels = deviceModels,
                    CustomDevices = customDevices
                }
            });

            this.clusteringConfig.Setup(x => x.MaxDevicesPerNode).Returns(50);

            this.TheCurrentNodeIsMaster();

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            // Verify request to update autoscale settings is made when node count changes
            this.azureManagementAdapterClient.Verify(x => x.CreateOrUpdateVmssAutoscaleSettingsAsync(It.Is<int>(a => a.Equals(expectedNodeCount))));

            // Arrange
            this.azureManagementAdapterClient.Invocations.Clear();

            // Act
            this.target.StartAsync(CancellationToken.None).CompleteOrTimeout();

            // Assert
            // Verify request to update autoscale settings is not made when node count does not change
            this.azureManagementAdapterClient.Verify(x => x.CreateOrUpdateVmssAutoscaleSettingsAsync(It.IsAny<int>()), Times.Never);
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

        private void TheCurrentNodeIsMaster()
        {
            this.clusterNodes.Setup(x => x.SelfElectToMasterNodeAsync()).ReturnsAsync(true);
        }

        private void TheCurrentNodeIsNotMaster()
        {
            this.clusterNodes.Setup(x => x.SelfElectToMasterNodeAsync()).ReturnsAsync(false);
        }
    }
}
