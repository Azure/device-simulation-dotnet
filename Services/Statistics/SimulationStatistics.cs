// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
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
        Task UpdateAsync(string simulationId, SimulationStatisticsModel statistics);
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

        /// <summary>
        /// Fetches statististics records for a given simulation and returns aggregate values.
        /// </summary>
        public async Task<SimulationStatisticsModel> GetSimulationStatisticsAsync(string simulationId)
        {
            if (string.IsNullOrEmpty(simulationId))
            {
                this.log.Error("Simulation Id cannot be null or empty");
                throw new InvalidInputException("Simulation Id cannot be null or empty");
            }

            SimulationStatisticsModel statistics = new SimulationStatisticsModel();

            try
            {
                var simulationRecords = (await this.simulationStatisticsStorage.GetAllAsync())
                    .Select(p => JsonConvert.DeserializeObject<SimulationStatisticsRecord>(p.Data))
                    .Where(i => i.SimulationId == simulationId)
                    .ToList();

                foreach (var record in simulationRecords)
                {
                    statistics.ActiveDevices += record.Statistics.ActiveDevices;
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
        
        /// <summary>
        /// Creates or updates statistics record for a given simulation.
        /// </summary>
        public async Task CreateOrUpdateAsync(string simulationId, SimulationStatisticsModel statistics)
        {
            var nodeId = this.clusterNodes.GetCurrentNodeId();
            var statisticsRecordId = this.GetStatisticsRecordId(simulationId, nodeId);
            var statisticsStorageRecord = this.GetStorageRecord(simulationId, statistics);

            try
            {
                if (await this.simulationStatisticsStorage.ExistsAsync(statisticsRecordId))
                {
                    this.log.Debug("Updating statistics record", () => new { statisticsStorageRecord });
                    var record = await this.simulationStatisticsStorage.GetAsync(statisticsRecordId);
                    await this.simulationStatisticsStorage.UpsertAsync(statisticsStorageRecord, record.ETag);
                }
                else
                {
                    this.log.Debug("Creating statistics record", () => new { statisticsStorageRecord });
                    await this.simulationStatisticsStorage.CreateAsync(statisticsStorageRecord);
                }
            }
            catch (Exception e)
            {
                this.log.Error("Error on saving statistics records", e);
            }
        }

        /// <summary>
        /// Updates statistics record for a given simulation.
        /// </summary>
        public async Task UpdateAsync(string simulationId, SimulationStatisticsModel statistics)
        {
            var nodeId = this.clusterNodes.GetCurrentNodeId();
            var statisticsRecordId = this.GetStatisticsRecordId(simulationId, nodeId);
            var statisticsStorageRecord = this.GetStorageRecord(simulationId, statistics);

            try
            {
                this.log.Debug("Updating statistics record", () => new { statisticsStorageRecord });
                var record = await this.simulationStatisticsStorage.GetAsync(statisticsRecordId);
                await this.simulationStatisticsStorage.UpsertAsync(statisticsStorageRecord, record.ETag);
            }
            catch (Exception e)
            {
                this.log.Error("Error on saving statistics records", e);
            }
        }

        /// <summary>
        /// Deletes statistics records for a simulation.
        /// </summary>
        public async Task DeleteSimulationStatisticsAsync(string simulationId)
        {
            if (string.IsNullOrEmpty(simulationId))
            {
                this.log.Error("Simulation Id cannot be null or empty");
                throw new InvalidInputException("Simulation Id cannot be null or empty");
            }

            SimulationStatisticsModel statistics = new SimulationStatisticsModel();

            try
            {
                var statisticsRecordsIds = (await this.simulationStatisticsStorage.GetAllAsync())
                    .Select(r => r.Id)
                    .Where(i => i.StartsWith(simulationId))
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

        private StorageRecord GetStorageRecord(string simulationId, SimulationStatisticsModel statistics)
        {
            var nodeId = this.clusterNodes.GetCurrentNodeId();
            var statisticsRecordId = this.GetStatisticsRecordId(simulationId, nodeId);

            var statisticsRecord = new SimulationStatisticsRecord
            {
                NodeId = nodeId,
                SimulationId = simulationId,
                Statistics = statistics
            };

            return new StorageRecord
            {
                Id = statisticsRecordId,
                Data = JsonConvert.SerializeObject(statisticsRecord)
            };
        }

        private string GetStatisticsRecordId(string simId, string nodeId)
        {
            return $"{simId}__{nodeId}";
        }
    }
}
