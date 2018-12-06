// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Statistics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.CosmosDbSql;
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
        private List<IDataRecord> storageRecords;

        private readonly SimulationStatistics target;

        private readonly Mock<IClusterNodes> clusterNodes;
        private readonly Mock<ILogger> log;
        private readonly Mock<IEngines> enginesFactory;
        private readonly Mock<IServicesConfig> config;
        private readonly Mock<IEngine> simulationStatisticsStorage;

        public SimulationStatisticsTest()
        {
            var STATISTICS = "statistics";

            this.clusterNodes = new Mock<IClusterNodes>();
            this.log = new Mock<ILogger>();
            this.simulationStatisticsStorage = new Mock<IEngine>();
            this.enginesFactory = new Mock<IEngines>();
            this.config = new Mock<IServicesConfig>();
            this.config.SetupGet(x => x.StatisticsStorage)
                .Returns(new Config { CosmosDbSqlCollection = STATISTICS });

            this.simulationStatisticsStorage
                .Setup(x => x.Init(It.Is<Config>(c => c.CosmosDbSqlCollection == STATISTICS)))
                .Returns(this.simulationStatisticsStorage.Object);
            this.simulationStatisticsStorage
                .Setup(x => x.BuildRecord(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string id, string json) => new DataRecord { Id = id, Data = json });

            this.enginesFactory.Setup(x => x.Build(It.IsAny<Config>()))
                .Returns(this.simulationStatisticsStorage.Object);

            this.target = new SimulationStatistics(
                this.config.Object,
                this.clusterNodes.Object,
                this.enginesFactory.Object,
                this.log.Object);

            // Mock storage records
            this.storageRecords = new List<IDataRecord>
            {
                new DataRecord
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
                new DataRecord
                {
                    Id = $"{SIM_ID}__{NODE_IDS[1]}",
                    Data = JsonConvert.SerializeObject(
                        new SimulationStatisticsRecord
                        {
                            SimulationId = SIM_ID,
                            NodeId = NODE_IDS[1],
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

            this.clusterNodes
                .Setup(x => x.GetSortedIdListAsync())
                .ReturnsAsync(new SortedSet<string> { NODE_IDS[0], NODE_IDS[1] });

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
        public void ItIgnoresAverageCountForExpiredNode()
        {
            // Arrange
            SimulationStatisticsModel expectedStatistics = new SimulationStatisticsModel
            {
                ActiveDevices = 10,
                TotalMessagesSent = 300,
                FailedDeviceConnections = 6,
                FailedDevicePropertiesUpdates = 8,
                FailedMessages = 10
            };

            this.simulationStatisticsStorage
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(this.storageRecords);

            this.clusterNodes
                .Setup(x => x.GetSortedIdListAsync())
                .ReturnsAsync(new SortedSet<string> { NODE_IDS[1] });

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

            SimulationStatisticsModel statistics = new SimulationStatisticsModel
            {
                ActiveDevices = 5,
                TotalMessagesSent = 300,
                FailedDeviceConnections = 6,
                FailedDevicePropertiesUpdates = 8,
                FailedMessages = 10
            };

            SimulationStatisticsRecord statisticsRecord = new SimulationStatisticsRecord
            {
                NodeId = NODE_IDS[0],
                SimulationId = SIM_ID,
                Statistics = statistics
            };

            IDataRecord storageRecord = new DataRecord
            {
                Id = statisticsRecordId,
                Data = JsonConvert.SerializeObject(statisticsRecord)
            };

            this.clusterNodes.Setup(x => x.GetCurrentNodeId()).Returns(NODE_IDS[0]);

            this.simulationStatisticsStorage.Setup(x => x.ExistsAsync(NODE_IDS[0])).ReturnsAsync(false);

            // Act
            this.target.CreateOrUpdateAsync(SIM_ID, statistics).CompleteOrTimeout();

            // Assert
            this.simulationStatisticsStorage.Verify(x => x.CreateAsync(It.Is<IDataRecord>(
                a => a.GetId() == storageRecord.GetId() &&
                     a.GetData() == storageRecord.GetData())));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItUpdatesSimulationStatisticsIfRecordExists()
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

            IDataRecord storageRecord = new DataRecord
            {
                Id = statisticsRecordId,
                Data = JsonConvert.SerializeObject(expectedStatistics),
            };

            this.clusterNodes.Setup(x => x.GetCurrentNodeId()).Returns(NODE_IDS[0]);
            this.simulationStatisticsStorage.Setup(x => x.ExistsAsync(statisticsRecordId)).ReturnsAsync(true);
            this.simulationStatisticsStorage.Setup(x => x.GetAsync(statisticsRecordId)).ReturnsAsync(storageRecord);

            // Act
            this.target.CreateOrUpdateAsync(SIM_ID, inputStatistics).CompleteOrTimeout();

            // Assert
            this.simulationStatisticsStorage.Verify(x => x.GetAsync(It.Is<string>(
                a => a == statisticsRecordId)));
            this.simulationStatisticsStorage.Verify(x => x.UpsertAsync(It.Is<IDataRecord>(
                    a => a.GetId() == storageRecord.GetId() &&
                         a.GetData() == storageRecord.GetData()),
                It.IsAny<string>()));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItUpdatesSimulationStatistics()
        {
            // Arrange
            var statisticsRecordId = $"{SIM_ID}__{NODE_IDS[0]}";

            var inputStatistics = new SimulationStatisticsModel
            {
                ActiveDevices = 5,
                TotalMessagesSent = 300,
                FailedDeviceConnections = 6,
                FailedDevicePropertiesUpdates = 8,
                FailedMessages = 10
            };

            var expectedStatistics = new SimulationStatisticsRecord
            {
                SimulationId = SIM_ID,
                NodeId = NODE_IDS[0],
                Statistics = inputStatistics
            };

            IDataRecord newRecord = new DataRecord
            {
                Id = statisticsRecordId,
                Data = JsonConvert.SerializeObject(expectedStatistics),
            };

            this.clusterNodes.Setup(x => x.GetCurrentNodeId()).Returns(NODE_IDS[0]);
            this.simulationStatisticsStorage.Setup(x => x.GetAsync(statisticsRecordId)).ReturnsAsync(newRecord);

            // Act
            var result = this.target.UpdateAsync(SIM_ID, inputStatistics).CompleteOrTimeout();

            // Assert
            this.simulationStatisticsStorage.Verify(x => x.UpsertAsync(It.Is<IDataRecord>(
                    a => a.GetId() == newRecord.GetId() &&
                         a.GetData() == newRecord.GetData()),
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
