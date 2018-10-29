// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Statistics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Moq;
using Newtonsoft.Json;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.Statistics
{
    public class SimulationStatisticsTest
    {
        private const string SIM_ID = "1";
        private static string[] NODE_IDS = { "123", "234" };
        private readonly SimulationStatistics target;
        private readonly Mock<IClusterNodes> clusterNodes;
        private readonly Mock<ILogger> log;
        private readonly Mock<IFactory> factory;
        private readonly Mock<IServicesConfig> config;
        private readonly Mock<IStorageRecords> simulationStatisticsStorage;
        private List<StorageRecord> storageRecords;

        public SimulationStatisticsTest()
        {
            var STATISTICS = "statistics";
            this.clusterNodes = new Mock<IClusterNodes>();
            this.log = new Mock<ILogger>();
            this.simulationStatisticsStorage = new Mock<IStorageRecords>();
            this.factory = new Mock<IFactory>();
            this.config = new Mock<IServicesConfig>();
            this.config.SetupGet(x => x.StatisticsStorage)
                .Returns(new StorageConfig { DocumentDbCollection = STATISTICS });

            this.simulationStatisticsStorage
                .Setup(x => x.Init(It.Is<StorageConfig>(c => c.DocumentDbCollection == STATISTICS)))
                .Returns(this.simulationStatisticsStorage.Object);
            this.factory.Setup(x => x.Resolve<IStorageRecords>()).Returns(this.simulationStatisticsStorage.Object);

            this.target = new SimulationStatistics(
                this.config.Object,
                this.clusterNodes.Object,
                this.factory.Object,
                this.log.Object);

            // Mock storage records
            this.storageRecords = new List<StorageRecord>
            {
                new StorageRecord
                {
                    Id = $"{SIM_ID}__{NODE_IDS[0]}",
                    Data = JsonConvert.SerializeObject(
                        new SimulationStatisticsRecord
                        {
                            SimulationId = SIM_ID,
                            NodeId = NODE_IDS[0],
                            Statistics = new SimulationStatisticsModel
                            {
                                ActiveDevices = 5,
                                TotalMessagesSent = 100,
                                FailedDeviceConnections = 1,
                                FailedDevicePropertiesUpdates = 2,
                                FailedMessages = 3
                            }
                        })
                },
                new StorageRecord
                {
                    Id = $"{SIM_ID}__{NODE_IDS[1]}",
                    Data = JsonConvert.SerializeObject(
                        new SimulationStatisticsRecord
                        {
                            SimulationId = SIM_ID,
                            NodeId = NODE_IDS[0],
                            Statistics = new SimulationStatisticsModel
                            {
                                ActiveDevices = 10,
                                TotalMessagesSent = 200,
                                FailedDeviceConnections = 5,
                                FailedDevicePropertiesUpdates = 6,
                                FailedMessages = 7
                            }
                        })
                },
            };
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItGetsSimulationStatistics()
        {
            // Arrange
            SimulationStatisticsModel expectedStatistics = new SimulationStatisticsModel
            {
                ActiveDevices = 15,
                TotalMessagesSent = 300,
                FailedDeviceConnections = 6,
                FailedDevicePropertiesUpdates = 8,
                FailedMessages = 10
            };

            this.simulationStatisticsStorage
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(this.storageRecords);

            // Act
            var result = this.target.GetSimulationStatisticsAsync(SIM_ID).CompleteOrTimeout();

            // Assert
            Assert.Equal(expectedStatistics.ActiveDevices, result.Result.ActiveDevices);
            Assert.Equal(expectedStatistics.TotalMessagesSent, result.Result.TotalMessagesSent);
            Assert.Equal(expectedStatistics.FailedDeviceConnections, result.Result.FailedDeviceConnections);
            Assert.Equal(expectedStatistics.FailedDevicePropertiesUpdates, result.Result.FailedDevicePropertiesUpdates);
            Assert.Equal(expectedStatistics.FailedMessages, result.Result.FailedMessages);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesSimulationStatistics()
        {
            // Arrange
            var statisticsRecordId = $"{SIM_ID}__{NODE_IDS[0]}";

            SimulationStatisticsModel inputStatistics = new SimulationStatisticsModel
            {
                ActiveDevices = 5,
                TotalMessagesSent = 300,
                FailedDeviceConnections = 6,
                FailedDevicePropertiesUpdates = 8,
                FailedMessages = 10
            };

            SimulationStatisticsRecord expectedStatistics = new SimulationStatisticsRecord
            {
                SimulationId = SIM_ID,
                NodeId = NODE_IDS[0],
                Statistics = inputStatistics
            };

            StorageRecord storageRecord = new StorageRecord
            {
                Id = statisticsRecordId,
                Data = JsonConvert.SerializeObject(expectedStatistics)
            };

            this.clusterNodes.Setup(x => x.GetCurrentNodeId()).Returns(NODE_IDS[0]);
            this.simulationStatisticsStorage.Setup(x => x.ExistsAsync(NODE_IDS[0])).ReturnsAsync(false);

            // Act
            var result = this.target.CreateOrUpdateAsync(SIM_ID, inputStatistics).CompleteOrTimeout();

            // Assert
            this.simulationStatisticsStorage.Verify(x => x.CreateAsync(It.Is<StorageRecord>(
                a => a.Id == storageRecord.Id &&
                     a.Data == storageRecord.Data)));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItUpdatesSimulationStatistics()
        {
            // Arrange
            var statisticsRecordId = $"{SIM_ID}__{NODE_IDS[0]}";

            SimulationStatisticsModel inputStatistics = new SimulationStatisticsModel
            {
                ActiveDevices = 5,
                TotalMessagesSent = 300,
                FailedDeviceConnections = 6,
                FailedDevicePropertiesUpdates = 8,
                FailedMessages = 10
            };

            SimulationStatisticsRecord expectedStatistics = new SimulationStatisticsRecord
            {
                SimulationId = SIM_ID,
                NodeId = NODE_IDS[0],
                Statistics = inputStatistics
            };

            StorageRecord storageRecord = new StorageRecord
            {
                Id = statisticsRecordId,
                Data = JsonConvert.SerializeObject(expectedStatistics),
            };

            this.clusterNodes.Setup(x => x.GetCurrentNodeId()).Returns(NODE_IDS[0]);
            this.simulationStatisticsStorage.Setup(x => x.ExistsAsync(statisticsRecordId)).ReturnsAsync(true);
            this.simulationStatisticsStorage.Setup(x => x.GetAsync(statisticsRecordId)).ReturnsAsync(storageRecord);

            // Act
            var result = this.target.CreateOrUpdateAsync(SIM_ID, inputStatistics).CompleteOrTimeout();

            // Assert
            this.simulationStatisticsStorage.Verify(x => x.GetAsync(It.Is<string>(
               a => a == statisticsRecordId)));
            this.simulationStatisticsStorage.Verify(x => x.UpsertAsync(It.Is<StorageRecord>(
                a => a.Id == storageRecord.Id &&
                     a.Data == storageRecord.Data),
                     It.IsAny<string>()));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDeletesSimulationStatistics()
        {
            // Arrange
            List<string> expectedIds = new List<string>(new string[] { $"{SIM_ID}__{NODE_IDS[0]}", $"{SIM_ID}__{NODE_IDS[1]}" });

            this.simulationStatisticsStorage
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(this.storageRecords);

            // Act
            this.target.DeleteSimulationStatisticsAsync(SIM_ID).CompleteOrTimeout();

            // Assert
            this.simulationStatisticsStorage.Verify(x => x.DeleteMultiAsync(It.Is<List<string>>(
                a => a.Count.Equals(expectedIds.Count))));
        }
    }
}
