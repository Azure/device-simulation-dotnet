// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Statistics
{
    public interface ISimulationStatistics
    {
        Task<SimulationStatisticsModel> GetSimulationStatisticsAsync(string simulationId);
        Task CreateOrUpdateAsync(string simulationId, SimulationStatisticsModel statistics);
    }

    public class SimulationStatisticsAgent : ISimulationStatistics
    {
        private readonly IStorageRecords simulationStatisticsStorage;
        private readonly IClusterNodes clusterNodes;
        private readonly ISimulations simulations;
        private readonly IServicesConfig config;
        private readonly ILogger log;

        public SimulationStatisticsAgent(IServicesConfig config,
            IClusterNodes clusterNodes,
            ISimulations simulations,
            IFactory factory,
            ILogger logger)
        {
            this.clusterNodes = clusterNodes;
            this.simulations = simulations;
            this.config = config;
            this.log = logger;
            this.simulationStatisticsStorage = factory.Resolve<IStorageRecords>().Init(config.StatisticsStorage);
        }

        public async Task<SimulationStatisticsModel> GetSimulationStatisticsAsync(string simulationId)
        {
            string sqlCondition = " CONTAINS(ROOT.id, @simulationId)";
            SqlParameter[] sqlParameters = new[] { new SqlParameter { Name = "@simulationId", Value = simulationId } };

            var simRecords = (await this.simulationStatisticsStorage.GetAsync(sqlCondition, sqlParameters))
                .Select(p => JsonConvert.DeserializeObject<SimulationStatisticsRecord>(p.Data))
                .ToList();

            SimulationStatisticsModel statistics = new SimulationStatisticsModel();
            statistics.TotalMessagesSent = simRecords.Sum(a => a.Statistics.TotalMessagesSent);
            statistics.FailedDeviceConnectionsCount = simRecords.Sum(a => a.Statistics.FailedDeviceConnectionsCount);
            statistics.FailedDeviceTwinUpdatesCount = simRecords.Sum(a => a.Statistics.FailedDeviceTwinUpdatesCount);
            statistics.FailedMessagesCount = simRecords.Sum(a => a.Statistics.FailedMessagesCount);

            return statistics;
        }
        
        public async Task CreateOrUpdateAsync(string simulationId, SimulationStatisticsModel statistics)
        {
            var nodeId = this.clusterNodes.GetCurrentNodeId();
            var statisticsRecordId = this.GetStatisticsRecordId(simulationId, nodeId);

            var stats = new SimulationStatisticsRecord
            {
                NodeId = nodeId,
                SimulationId = simulationId,
                Statistics = statistics
            };

            var statsRecord = new StorageRecord
            {
                Id = statisticsRecordId,
                Data = JsonConvert.SerializeObject(stats)
            };

            await this.simulationStatisticsStorage.CreateAsync(statsRecord);
        }

        private string GetStatisticsRecordId(string simId, string nodeId)
        {
            return $"{simId}__{nodeId}";
        }
    }
}
