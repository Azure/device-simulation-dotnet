// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Statistics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceReplay;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;
using Moq;
using SimulationAgent.Test.helpers;
using Xunit;

namespace SimulationAgent.Test
{
    public class SimulationManagerTest
    {
        private const string SIM_ID = "1";
        private const int EXPECTED_ACTOR_COUNT = 3;
        private const int EXPECTED_PARTITION_COUNT = 8;
        private const int MAX_DEVICES_PER_NODE = EXPECTED_ACTOR_COUNT * EXPECTED_PARTITION_COUNT;
        private const string MODEL1 = "foo-bar";
        private const string MODEL2 = "bar-baz";
        private const string DEVICE1 = "j1";
        private const string DEVICE2 = "j2";
        private const string DEVICE3 = "b3";
        private const string ACTOR_PREFIX_SEPARATOR = "//";

        private readonly Mock<ISimulationContext> mockSimulationContext;
        private readonly Mock<IDevicePartitions> mockDevicePartitions;
        private readonly Mock<IClusterNodes> mockClusterNodes;
        private readonly Mock<IDeviceModels> mockDeviceModels;
        private readonly Mock<IFactory> mockFactory;
        private readonly Mock<IClusteringConfig> mockClusteringConfig;
        private readonly Mock<ILogger> mockLogger;
        private readonly Mock<IInstance> mockInstance;
        private readonly Mock<ISimulations> mockSimulations;
        private readonly Mock<ISimulationStatistics> mockSimulationStatistics;
        private readonly ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors;
        private readonly ConcurrentDictionary<string, IDeviceConnectionActor> mockDeviceContext;
        private readonly ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors;
        private readonly ConcurrentDictionary<string, IDevicePropertiesActor> devicePropertiesActors;
        private readonly ConcurrentDictionary<string, IDeviceReplayActor> deviceReplayActors;

        private SimulationManager target;

        public SimulationManagerTest()
        {
            this.mockSimulationContext = new Mock<ISimulationContext>();
            this.mockDevicePartitions = new Mock<IDevicePartitions>();
            this.mockClusterNodes = new Mock<IClusterNodes>();
            this.mockDeviceModels = new Mock<IDeviceModels>();
            this.mockFactory = new Mock<IFactory>();
            this.mockClusteringConfig = new Mock<IClusteringConfig>();
            this.mockClusteringConfig.SetupGet(x => x.MaxDevicesPerNode).Returns(MAX_DEVICES_PER_NODE);
            this.mockLogger = new Mock<ILogger>();
            this.mockInstance = new Mock<IInstance>();
            this.mockSimulations = new Mock<ISimulations>();
            this.mockSimulationStatistics = new Mock<ISimulationStatistics>();

            this.target = new SimulationManager(
                this.mockSimulationContext.Object,
                this.mockDevicePartitions.Object,
                this.mockClusterNodes.Object,
                this.mockDeviceModels.Object,
                this.mockFactory.Object,
                this.mockClusteringConfig.Object,
                this.mockLogger.Object,
                this.mockInstance.Object,
                this.mockSimulationStatistics.Object,
                this.mockSimulations.Object);

            // Initialize the target
            var simulation = new Simulation { Id = SIM_ID, PartitioningComplete = false };
            this.deviceStateActors = new ConcurrentDictionary<string, IDeviceStateActor>();
            this.mockDeviceContext = new ConcurrentDictionary<string, IDeviceConnectionActor>();
            this.deviceTelemetryActors = new ConcurrentDictionary<string, IDeviceTelemetryActor>();
            this.devicePropertiesActors = new ConcurrentDictionary<string, IDevicePropertiesActor>();
            this.deviceReplayActors = new ConcurrentDictionary<string, IDeviceReplayActor>();

            this.target.InitAsync(
                simulation,
                this.deviceStateActors,
                this.mockDeviceContext,
                this.deviceTelemetryActors,
                this.devicePropertiesActors,
                this.deviceReplayActors).Wait(Constants.TEST_TIMEOUT);
        }

        [Fact]
        void ItTriesToRenewTheLockOnAssignedPartitions()
        {
            // Arrange
            this.SetupPartitionsAndModels();
            var newPartitionsTask = this.target.AssignNewPartitionsAsync();
            newPartitionsTask.Wait(Constants.TEST_TIMEOUT);

            // Act
            var holdPartitionsTask = this.target.HoldAssignedPartitionsAsync();
            holdPartitionsTask.Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.mockDevicePartitions.Verify(x => x.TryToKeepPartitionAsync(It.IsAny<string>()), Times.Exactly(EXPECTED_PARTITION_COUNT));
        }

        [Fact]
        void ItDeletesActorsForPartitionsBeingReleased()
        {
            // Arrange
            // Set up eight partitions, mock two of them not being renewed, then
            // ask the target to process these partitions, twice. The second time
            // through, we should only have six calls to TryToKeepPartitionAsync
            // as there should only be six partitions.
            this.SetupPartitionsAndModels();
            this.target.AssignNewPartitionsAsync().Wait(Constants.TEST_TIMEOUT);

            // Mock two partitions not being renewed
            this.mockDevicePartitions.Setup(x => x.TryToKeepPartitionAsync(It.IsIn("3", "7"))).ReturnsAsync(false);
            this.mockDevicePartitions.Setup(x => x.TryToKeepPartitionAsync(It.IsIn("1", "2", "4", "5", "6", "8"))).ReturnsAsync(true);

            // Arrange, Act
            this.mockDevicePartitions.Invocations.Clear();
            this.target.HoldAssignedPartitionsAsync().Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.mockDevicePartitions.Verify(x => x.TryToKeepPartitionAsync(It.IsAny<string>()), Times.Exactly(8));

            // Arrange, Act
            this.mockDevicePartitions.Invocations.Clear();
            this.target.HoldAssignedPartitionsAsync().Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.mockDevicePartitions.Verify(x => x.TryToKeepPartitionAsync(It.IsAny<string>()), Times.Exactly(6));
        }

        [Fact]
        void ItAssignsNewPartitionsAndCreatesActors()
        {
            // Arrange
            this.SetupPartitionsAndModels();

            // Act
            var targetTask = this.target.AssignNewPartitionsAsync();
            targetTask.Wait(Constants.TEST_TIMEOUT);

            // Assert
            Assert.Equal(EXPECTED_ACTOR_COUNT, this.deviceStateActors.Count);
            Assert.Equal(EXPECTED_ACTOR_COUNT, this.mockDeviceContext.Count);
            Assert.Equal(EXPECTED_ACTOR_COUNT, this.devicePropertiesActors.Count);
            Assert.Equal(EXPECTED_ACTOR_COUNT, this.deviceTelemetryActors.Count);

            this.mockDevicePartitions.Verify(x => x.TryToAssignPartitionAsync(It.IsAny<string>()), Times.Exactly(EXPECTED_PARTITION_COUNT));
        }

        [Fact]
        void ItWillNotAddDevicesIfNodeIsFull()
        {
            // Arrange
            this.SetupPartitionsAndModels();
            // Create some partitions and actors
            this.target.AssignNewPartitionsAsync().Wait(Constants.TEST_TIMEOUT);
            this.SetupPartitionsAndModels();

            // Act
            // Try to create more partitions and actors. No more should be created
            // as we should be at the max number of devices for a node.
            this.target.AssignNewPartitionsAsync().Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.mockFactory.Verify(x => x.Resolve<IDeviceStateActor>(), Times.Exactly(MAX_DEVICES_PER_NODE));
        }

        [Fact]
        void ItUpdatesThrottlingLimit()
        {
            // Arrange
            this.mockClusterNodes.Setup(x => x.GetSortedIdListAsync()).ReturnsAsync(
                new SortedSet<string> { "node1", "node2" }
            );
            var mockRateLimiting = new Mock<IRateLimiting>();
            this.mockSimulationContext.SetupGet(x => x.RateLimiting).Returns(mockRateLimiting.Object);

            // Act
            this.target.UpdateThrottlingLimitsAsync().Wait(Constants.TEST_TIMEOUT);

            // Assert
            mockRateLimiting.Verify(x => x.ChangeClusterSize(It.IsAny<int>()), Times.Once);
        }

        private void SetupPartitionsAndModels()
        {
            // Arrange
            // return a set of unassigned partitions
            var partitions = new List<DevicesPartition>();
            var deviceIdsByModel = new Dictionary<string, List<string>>
            {
                { MODEL1, new List<string> { DEVICE1, DEVICE2 } },
                { MODEL2, new List<string> { DEVICE3 } }
            };

            partitions.Add(new DevicesPartition { Id = "1", SimulationId = SIM_ID, DeviceIdsByModel = deviceIdsByModel });
            partitions.Add(new DevicesPartition { Id = "2", SimulationId = SIM_ID, DeviceIdsByModel = deviceIdsByModel });
            partitions.Add(new DevicesPartition { Id = "3", SimulationId = SIM_ID, DeviceIdsByModel = deviceIdsByModel });
            partitions.Add(new DevicesPartition { Id = "4", SimulationId = SIM_ID, DeviceIdsByModel = deviceIdsByModel });
            partitions.Add(new DevicesPartition { Id = "5", SimulationId = SIM_ID, DeviceIdsByModel = deviceIdsByModel });
            partitions.Add(new DevicesPartition { Id = "6", SimulationId = SIM_ID, DeviceIdsByModel = deviceIdsByModel });
            partitions.Add(new DevicesPartition { Id = "7", SimulationId = SIM_ID, DeviceIdsByModel = deviceIdsByModel });
            partitions.Add(new DevicesPartition { Id = "8", SimulationId = SIM_ID, DeviceIdsByModel = deviceIdsByModel });

            this.mockDevicePartitions.Setup(x => x.GetUnassignedAsync(It.IsAny<string>())).ReturnsAsync(partitions);

            // report that locking partitions was successful
            this.mockDevicePartitions.Setup(x => x.TryToAssignPartitionAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Set up a mock device model, with sufficient properties set to allow creation of a telemetry actor
            this.mockDeviceModels.Setup(x => x.GetWithOverrideAsync(It.IsAny<string>(), It.IsAny<Simulation>()))
                .ReturnsAsync(
                    new DeviceModel
                    {
                        Telemetry = new List<DeviceModel.DeviceModelMessage>
                        {
                            new DeviceModel.DeviceModelMessage
                            {
                                Interval = TimeSpan.FromSeconds(RandomInt()),
                            }
                        }
                    }
                );

            // Creation of mock actors
            var mockDeviceStateActor = new Mock<IDeviceStateActor>();
            var mockDeviceConnectionActor = new Mock<IDeviceConnectionActor>();
            var mockDevicePropertiesActor = new Mock<IDevicePropertiesActor>();
            var mockDeviceTelemetryActor = new Mock<IDeviceTelemetryActor>();

            this.mockFactory.Setup(x => x.Resolve<IDeviceStateActor>()).Returns(mockDeviceStateActor.Object);
            this.mockFactory.Setup(x => x.Resolve<IDeviceConnectionActor>()).Returns(mockDeviceConnectionActor.Object);
            this.mockFactory.Setup(x => x.Resolve<IDevicePropertiesActor>()).Returns(mockDevicePropertiesActor.Object);
            this.mockFactory.Setup(x => x.Resolve<IDeviceTelemetryActor>()).Returns(mockDeviceTelemetryActor.Object);
        }

        [Fact]
        void ItDeletesAllActorsAtTearDown()
        {
            // Arrange
            this.SetupPartitionsAndModels();

            // Act
            this.target.TearDown();

            // Assert
            Assert.Empty(this.deviceStateActors);
            Assert.Empty(this.mockDeviceContext);
            Assert.Empty(this.deviceTelemetryActors);
            Assert.Empty(this.devicePropertiesActors);
        }

        [Fact]
        void ItCalculatesStatsAndInvokesCreateOrUpdateStatistics()
        {
            // Arrange
            var expectedTotalMessageCount = 200;
            var expectedFailedMessagesCount = 2;
            var expectedFailedDeviceConnectionsCount = 6;
            var expectedFailedTwinUpdatesCount = 10;
            var expectedActiveDevicesCount = 2;

            var statisticsModel = new SimulationStatisticsModel
            {
                ActiveDevices = expectedActiveDevicesCount,
                TotalMessagesSent = expectedTotalMessageCount,
                FailedMessages = expectedFailedMessagesCount,
                FailedDeviceConnections = expectedFailedDeviceConnectionsCount,
                FailedDevicePropertiesUpdates = expectedFailedTwinUpdatesCount
            };

            // Add data for expected simulation
            for (int i = 0; i < 2; i++)
            {
                var deviceName = SIM_ID + ACTOR_PREFIX_SEPARATOR + i;
                var mockDeviceTelemetryActor = new Mock<IDeviceTelemetryActor>();
                mockDeviceTelemetryActor.Setup(x => x.TotalMessagesCount).Returns(100);
                mockDeviceTelemetryActor.Setup(x => x.FailedMessagesCount).Returns(1);
                this.deviceTelemetryActors.TryAdd(deviceName, mockDeviceTelemetryActor.Object);

                var mockDeviceConnectionActor = new Mock<IDeviceConnectionActor>();
                mockDeviceConnectionActor.Setup(x => x.FailedDeviceConnectionsCount).Returns(3);
                this.mockDeviceContext.TryAdd(deviceName, mockDeviceConnectionActor.Object);

                var mockDevicePropertiesActor = new Mock<IDevicePropertiesActor>();
                mockDevicePropertiesActor.Setup(x => x.FailedTwinUpdatesCount).Returns(5);
                this.devicePropertiesActors.TryAdd(deviceName, mockDevicePropertiesActor.Object);

                var mockDeviceStateActor = new Mock<IDeviceStateActor>();
                mockDeviceStateActor.Setup(x => x.IsDeviceActive).Returns(true);
                this.deviceStateActors.TryAdd(deviceName, mockDeviceStateActor.Object);
            }

            // Add data for additional simulations
            for (int i = 2; i < 5; i++)
            {
                var deviceName = SIM_ID + i + ACTOR_PREFIX_SEPARATOR + i;
                var mockDeviceTelemetryActor = new Mock<IDeviceTelemetryActor>();
                mockDeviceTelemetryActor.Setup(x => x.TotalMessagesCount).Returns(100 * i);
                mockDeviceTelemetryActor.Setup(x => x.FailedMessagesCount).Returns(1 * i);
                this.deviceTelemetryActors.TryAdd(deviceName, mockDeviceTelemetryActor.Object);

                var mockDeviceConnectionActor = new Mock<IDeviceConnectionActor>();
                mockDeviceConnectionActor.Setup(x => x.FailedDeviceConnectionsCount).Returns(3 * i);
                this.mockDeviceContext.TryAdd(deviceName, mockDeviceConnectionActor.Object);

                var mockDevicePropertiesActor = new Mock<IDevicePropertiesActor>();
                mockDevicePropertiesActor.Setup(x => x.FailedTwinUpdatesCount).Returns(5 * i);
                this.devicePropertiesActors.TryAdd(deviceName, mockDevicePropertiesActor.Object);

                var mockDeviceStateActor = new Mock<IDeviceStateActor>();
                mockDeviceStateActor.Setup(x => x.IsDeviceActive).Returns(true);
                this.deviceStateActors.TryAdd(deviceName, mockDeviceStateActor.Object);
            }

            // Act
            this.target.SaveStatisticsAsync().CompleteOrTimeout(); 

            // Assert create or update is called with expected simulation id and statistics
            this.mockSimulationStatistics.Verify(x => x.CreateOrUpdateAsync(SIM_ID, It.Is<SimulationStatisticsModel>(
                a => a.ActiveDevices == statisticsModel.ActiveDevices &&
                     a.TotalMessagesSent == statisticsModel.TotalMessagesSent &&
                     a.FailedMessages == statisticsModel.FailedMessages &&
                     a.FailedDeviceConnections == statisticsModel.FailedDeviceConnections &&
                     a.FailedDevicePropertiesUpdates == statisticsModel.FailedDevicePropertiesUpdates)), Times.Once);
        }

        private static long RandomInt()
        {
            return Guid.NewGuid().ToByteArray().First();
        }
    }
}
