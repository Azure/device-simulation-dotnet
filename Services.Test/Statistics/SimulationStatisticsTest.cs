// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
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
        private readonly Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Statistics.SimulationStatistics target;
        private readonly Mock<IClusterNodes> clusterNodes;
        private readonly Mock<ISimulations> simulations;
        private readonly Mock<ILogger> log;
        private readonly Mock<IFactory> factory;
        private readonly Mock<IServicesConfig> config;
        private readonly Mock<IStorageRecords> simulationStatisticsStorage;
        
        public SimulationStatisticsTest()
        {
            var STATISTICS = "statistics";
            this.clusterNodes = new Mock<IClusterNodes>();
            this.simulations = new Mock<ISimulations>();
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

            this.target = new Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Statistics.SimulationStatistics(
                this.config.Object,
                this.clusterNodes.Object,
                this.simulations.Object,
                this.factory.Object,
                this.log.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItGetsSimulationStatistics()
        {
            // Arrange
            var simId = "1";

            SimulationStatisticsModel expectedStatistics = new SimulationStatisticsModel
            {
                TotalMessagesSent = 300,
                FailedDeviceConnectionsCount = 6,
                FailedDeviceTwinUpdatesCount = 8,
                FailedMessagesCount = 10
            };

            // Mock storage records
            var list = new List<StorageRecord>
            {
                new StorageRecord { Data = JsonConvert.SerializeObject(
                    new SimulationStatisticsRecord {
                        SimulationId = "1_1",
                        NodeId = "123",
                        Statistics = new SimulationStatisticsModel {
                            TotalMessagesSent = 100,
                            FailedDeviceConnectionsCount = 1,
                            FailedDeviceTwinUpdatesCount = 2,
                            FailedMessagesCount = 3 }})},
                new StorageRecord { Data = JsonConvert.SerializeObject(
                    new SimulationStatisticsRecord {
                        SimulationId = "1_2",
                        NodeId = "234",
                        Statistics = new SimulationStatisticsModel {
                            TotalMessagesSent = 200,
                            FailedDeviceConnectionsCount = 5,
                            FailedDeviceTwinUpdatesCount = 6,
                            FailedMessagesCount = 7 }})},
            };

            this.simulationStatisticsStorage
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<SqlParameter[]>()))
                .ReturnsAsync(list);

            // Act
            var result = this.target.GetSimulationStatisticsAsync(simId);

            // Assert
            Assert.Equal(expectedStatistics.TotalMessagesSent, result.Result.TotalMessagesSent);
            Assert.Equal(expectedStatistics.FailedDeviceConnectionsCount, result.Result.FailedDeviceConnectionsCount);
            Assert.Equal(expectedStatistics.FailedDeviceTwinUpdatesCount, result.Result.FailedDeviceTwinUpdatesCount);
            Assert.Equal(expectedStatistics.FailedMessagesCount, result.Result.FailedMessagesCount);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesSimulationStatistics()
        {
            // Arrange
            var simId = "1";
            var nodeId = simId + "123";
            var statisticsRecordId = $"{simId}__{nodeId}";

            SimulationStatisticsModel inputStatistics = new SimulationStatisticsModel
            {
                TotalMessagesSent = 300,
                FailedDeviceConnectionsCount = 6,
                FailedDeviceTwinUpdatesCount = 8,
                FailedMessagesCount = 10
            };

            SimulationStatisticsRecord expectedStatistics = new SimulationStatisticsRecord
            {
                SimulationId = simId,
                NodeId = nodeId,
                Statistics= inputStatistics
            };

            StorageRecord storageRecord = new StorageRecord
            {
                Id = statisticsRecordId,
                Data = JsonConvert.SerializeObject(expectedStatistics)
            };

            this.clusterNodes.Setup(x => x.GetCurrentNodeId()).Returns(nodeId);

            // Act
            var result = this.target.CreateOrUpdateAsync(simId, inputStatistics);

            // Assert
            this.simulationStatisticsStorage.Verify(x => x.CreateAsync(It.Is<StorageRecord>(
                a => a.Id == storageRecord.Id &&
                a.Data == storageRecord.Data)));
        }
    }
}
