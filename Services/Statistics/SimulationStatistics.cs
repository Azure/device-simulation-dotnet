// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
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
        Task DeleteSimulationStatisticsAsync(string simulationId);
    }

    public class SimulationStatistics : ISimulationStatistics
    {
        private readonly IStorageRecords simulationStatisticsStorage;
        private readonly IClusterNodes clusterNodes;
        private readonly IServicesConfig config;
        private readonly ILogger log;

        public SimulationStatistics(IServicesConfig config,
            IClusterNodes clusterNodes,
            IFactory factory,
            ILogger logger)
        {
            this.clusterNodes = clusterNodes;
            this.config = config;
            this.log = logger;
            this.simulationStatisticsStorage = factory.Resolve<IStorageRecords>().Init(config.StatisticsStorage);
        }

        public async Task<SimulationStatisticsModel> GetSimulationStatisticsAsync(string simulationId)
        {
            if (string.IsNullOrEmpty(simulationId))
            {
                this.log.Error("Simulation Id cannot be null or empty");
                return null;
            }

            string sqlCondition = " CONTAINS(ROOT.id, @simulationId)";
            SqlParameter[] sqlParameters = new[] { new SqlParameter { Name = "@simulationId", Value = simulationId } };
            SimulationStatisticsModel statistics = new SimulationStatisticsModel();

            try
            {
                var simulationRecords = (await this.simulationStatisticsStorage.GetAsync(sqlCondition, sqlParameters))
                    .Select(p => JsonConvert.DeserializeObject<SimulationStatisticsRecord>(p.Data))
                    .ToList();
                 
                foreach (var record in simulationRecords)
                {
                    statistics.TotalMessagesSent += record.Statistics.TotalMessagesSent;
                    statistics.FailedDeviceConnections += record.Statistics.FailedDeviceConnections;
                    statistics.FailedDevicePropertiesUpdates += record.Statistics.FailedDevicePropertiesUpdates;
                    statistics.FailedMessages += record.Statistics.FailedMessages;
                }
            }
            catch (Exception e)
            {
                this.log.Error("Error on getting statistics records", e);
            }
            
            return statistics;
        }
        
        public async Task CreateOrUpdateAsync(string simulationId, SimulationStatisticsModel statistics)
        {
            var nodeId = this.clusterNodes.GetCurrentNodeId();
            var statisticsRecordId = this.GetStatisticsRecordId(simulationId, nodeId);

            var statisticsRecord = new SimulationStatisticsRecord
            {
                NodeId = nodeId,
                SimulationId = simulationId,
                Statistics = statistics
            };

            var statisticsStorageRecord = new StorageRecord
            {
                Id = statisticsRecordId,
                Data = JsonConvert.SerializeObject(statisticsRecord)
            };

            try
            {
                this.log.Debug("Creating statistics record", () => new { statisticsStorageRecord });
                await this.simulationStatisticsStorage.CreateAsync(statisticsStorageRecord);
            }
            catch (Exception e)
            {
                this.log.Error("Error on saving statistics records", e);
            }
        }

        public async Task DeleteSimulationStatisticsAsync(string simulationId)
        {
            if (string.IsNullOrEmpty(simulationId))
            {
                this.log.Error("Simulation Id cannot be null or empty");
                return;
            }

            string sqlCondition = " CONTAINS(ROOT.id, @simulationId)";
            SqlParameter[] sqlParameters = new[] { new SqlParameter { Name = "@simulationId", Value = simulationId } };
            SimulationStatisticsModel statistics = new SimulationStatisticsModel();

            try
            {
                var statisticsRecordsIds = (await this.simulationStatisticsStorage.GetAsync(sqlCondition, sqlParameters))
                    .Select(p => p.Id)
                    .ToList();

                if (statisticsRecordsIds.Count > 0)
                {
                    this.log.Debug("Deleting statistics records", () => new { statisticsRecordsIds });
                    await this.simulationStatisticsStorage.DeleteMultiAsync(statisticsRecordsIds);
                }
                else
                {
                    this.log.Debug("No records to delete.");
                }
            }
            catch (Exception e)
            {
                this.log.Error("Error on getting statistics records", e);
            }
        }

        private string GetStatisticsRecordId(string simId, string nodeId)
        {
            return $"{simId}__{nodeId}";
        }
    }
}
