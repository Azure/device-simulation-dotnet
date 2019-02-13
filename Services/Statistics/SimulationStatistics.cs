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
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;

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
        private readonly IEngine simulationStatisticsStorage;
        private readonly IClusterNodes clusterNodes;
        private readonly IServicesConfig config;
        private readonly ILogger log;

        public SimulationStatistics(IServicesConfig config,
            IClusterNodes clusterNodes,
            IEngines engines,
            ILogger logger)
        {
            this.clusterNodes = clusterNodes;
            this.config = config;
            this.log = logger;
            this.simulationStatisticsStorage = engines.Build(config.StatisticsStorage);
        }

        /// <summary>
        /// Fetch statistics records for a given simulation and returns aggregate values.
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
                    .Select(p => JsonConvert.DeserializeObject<SimulationStatisticsRecord>(p.GetData()))
                    .Where(i => i.SimulationId == simulationId)
                    .ToList();

                var nodeRecords = await this.clusterNodes.GetSortedIdListAsync();

                foreach (var record in simulationRecords)
                {
                    if (nodeRecords.Contains(record.NodeId))
                    {
                        statistics.ActiveDevices += record.Statistics.ActiveDevices;
                    }

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
        /// Note: exceptions are caught upstream
        /// </summary>
        public async Task CreateOrUpdateAsync(string simulationId, SimulationStatisticsModel statistics)
        {
            var nodeId = this.clusterNodes.GetCurrentNodeId();
            string statisticsRecordId = this.BuildRecordId(simulationId, nodeId);
            IDataRecord statisticsStorageRecord = this.BuildStorageRecord(simulationId, statistics);

           /* if (await this.simulationStatisticsStorage.ExistsAsync(statisticsRecordId))
            {
                this.log.Debug("Updating statistics record", () => new { statisticsStorageRecord });
                var record = await this.simulationStatisticsStorage.GetAsync(statisticsRecordId);
                await this.simulationStatisticsStorage.UpsertAsync(statisticsStorageRecord, record.GetETag());
            }
            else
            */
            {
                this.log.Debug("Creating statistics record", () => new { statisticsStorageRecord });
             //   await this.simulationStatisticsStorage.CreateAsync(statisticsStorageRecord);
            }

            CloudBlockBlob blob;
            try
            {

                byte[] bytes = Encoding.UTF8.GetBytes(statistics.ConnectionStats);
                for (var i = 0; i < bytes.Length; i += 300000)
                {
                    int length = Math.Min(bytes.Length - i, 300000);
                    this.log.Debug("Writing stats to blob storage -- ", () => new { statistics.ConnectionStats });
                    blob = await this.WriteStatsToBlobAsync(statistics.ConnectionStats);
                }
            }
            catch (Exception e)
            {
                this.log.Error("Failed to create blob file required for saving stats", e);
                throw new ExternalDependencyException("Failed to create stats file", e);
            }
        }

        /// <summary>
        /// Updates statistics record for a given simulation.
        /// Note: exceptions are caught upstream
        /// </summary>
        public async Task UpdateAsync(string simulationId, SimulationStatisticsModel statistics)
        {
            var nodeId = this.clusterNodes.GetCurrentNodeId();
            var recordId = this.BuildRecordId(simulationId, nodeId);

            this.log.Debug("Fetch record to have latest ETag and overwrite the existing record", () => new { recordId });
            IDataRecord existingRecord = await this.simulationStatisticsStorage.GetAsync(recordId);

            this.log.Debug("Updating statistics record", () => new { recordId });
            IDataRecord newRecord = this.BuildStorageRecord(simulationId, statistics);
            await this.simulationStatisticsStorage.UpsertAsync(newRecord, existingRecord.GetETag());
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
                    .Select(r => r.GetId())
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

        private IDataRecord BuildStorageRecord(string simulationId, SimulationStatisticsModel statistics)
        {
            var nodeId = this.clusterNodes.GetCurrentNodeId();
            var statisticsRecordId = this.BuildRecordId(simulationId, nodeId);

            var statisticsRecord = new SimulationStatisticsRecord
            {
                NodeId = nodeId,
                SimulationId = simulationId,
                Statistics = statistics
            };

            return this.simulationStatisticsStorage.BuildRecord(
                statisticsRecordId, JsonConvert.SerializeObject(statisticsRecord));
        }

        private string BuildRecordId(string simId, string nodeId)
        {
           // var now = DateTime.Now.Ticks;
            return $"{simId}__{nodeId}";
        }

        private async Task<CloudBlockBlob> WriteStatsToBlobAsync(string sb)
        {
            // Write to blob
            string data = sb == null ? "" : sb;
            var blob = await this.CreateImportExportBlobAsync();
            using (var stream = await blob.OpenWriteAsync())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                for (var i = 0; i < bytes.Length; i += 190000)
                {
                    int length = Math.Min(bytes.Length - i, 190000);
                    await stream.WriteAsync(bytes, i, length);
                }
            }

            return blob;
        }

        private async Task<CloudBlockBlob> CreateImportExportBlobAsync()
        {
            // Container for the files managed by Azure IoT SDK.
            // Note: use a new container to speed up the operation and avoid old files left over
            string containerName = ("iothub-" + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-") + Guid.NewGuid().ToString("N")).ToLowerInvariant();
            string blobName = "stats.txt";

            this.log.Info("Creating import blob", () => new { containerName, blobName });

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(this.config.IoTHubImportStorageAccount);
            CloudBlobClient client = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(containerName);
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            await container.CreateIfNotExistsAsync();
            await blob.DeleteIfExistsAsync();

            return blob;
        }
    }
}
