// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering
{
    public interface IDevicePartitions
    {
        Task CreateAsync(string simulationId);

        // Find the partitions not assigned to any node
        Task<IList<DevicesPartition>> GetUnassignedAsync(string simulationId);

        Task<IList<DevicesPartition>> GetAllAsync();

        Task DeleteListAsync(List<string> partitionIds);

        // Lock the partition, so that the current node is the only one simulating its devices
        Task<bool> TryToAssignPartitionAsync(string partitionId);

        // Renew a lock on the partition
        Task<bool> TryToKeepPartitionAsync(string partitionId);
    }

    public class DevicePartitions : IDevicePartitions
    {
        private readonly ISimulations simulations;
        private readonly ILogger log;
        private readonly IEngine partitionsStorage;
        private readonly IClusterNodes clusterNodes;

        private readonly int partitionLockDurationSecs;
        private readonly int maxPartitionSize;

        public DevicePartitions(
            IServicesConfig config,
            IClusteringConfig clusteringConfig,
            ISimulations simulations,
            IClusterNodes clusterNodes,
            IEngines engines,
            ILogger logger)
        {
            this.simulations = simulations;
            this.log = logger;
            this.partitionsStorage = engines.Build(config.PartitionsStorage);
            this.clusterNodes = clusterNodes;
            this.partitionLockDurationSecs = clusteringConfig.PartitionLockDurationMsecs / 1000;
            this.maxPartitionSize = clusteringConfig.MaxPartitionSize;
        }

        public async Task CreateAsync(string simulationId)
        {
            this.log.Debug("Creating partitions...", () => new { simulationId });

            // Fetch latest record
            var simulation = await this.simulations.GetAsync(simulationId);
            if (simulation.PartitioningComplete)
            {
                this.log.Debug("Partitions created", () => new { simulationId });
                return;
            }

            // Find devices for the simulations to be partitioned
            Dictionary<string, List<string>> deviceIdsByModel = this.simulations.GetDeviceIdsByModel(simulation);
            var deviceCount = deviceIdsByModel.Select(x => x.Value.Count).Sum();
            this.log.Debug("Loaded device IDs", () => new { SimulationId = simulation.Id, deviceCount });

            // Delete partitions in case the previous partitioning setup crashed
            await this.DeletePartitionsAsync(simulation, deviceCount);

            // Create partitions
            await this.CreatePartitionsInternalAsync(simulation, deviceIdsByModel);
            this.log.Debug("The simulation partitioning is complete", () => new { SimulationId = simulation.Id });
            simulation.PartitioningComplete = true;
            await this.simulations.UpsertAsync(simulation, false);

            // Insert new devices, remove deleted devices
            // TODO

            this.log.Debug("Partitions created", () => new { simulationId });
        }

        // Return partitions ready to be simulated, not yet assigned to any node.
        public async Task<IList<DevicesPartition>> GetUnassignedAsync(string simulationId)
        {
            var nodeId = this.clusterNodes.GetCurrentNodeId();
            this.log.Debug("Searching partitions not assigned to any node...", () => new { simulationId, nodeId });
            var partitions = await this.partitionsStorage.GetAllAsync();

            var result = partitions
                .Where(p => !p.IsLocked())
                .Select(p => JsonConvert.DeserializeObject<DevicesPartition>(p.GetData()))
                .Where(x => x.SimulationId == simulationId)
                .ToList();

            this.log.Debug(result.Count > 0 ? "Unassigned partitions found" : "No unassigned partitions found",
                () => new { simulationId, count = result.Count });

            return result;
        }

        public async Task<IList<DevicesPartition>> GetAllAsync()
        {
            return (await this.partitionsStorage.GetAllAsync())
                .Select(p => JsonConvert.DeserializeObject<DevicesPartition>(p.GetData()))
                .ToList();
        }

        public async Task DeleteListAsync(List<string> partitionIds)
        {
            await this.partitionsStorage.DeleteMultiAsync(partitionIds);
        }

        // Lock the partition, so that the current node is the only one simulating its devices
        public async Task<bool> TryToAssignPartitionAsync(string partitionId)
        {
            var nodeId = this.clusterNodes.GetCurrentNodeId();

            this.log.Debug("Attempting to acquire partition...", () => new { partitionId, nodeId });
            var acquired = await this.partitionsStorage.TryToLockAsync(partitionId, nodeId, null, this.partitionLockDurationSecs);
            this.log.Debug(acquired ? "Partition acquired" : "Partition not acquired", () => new { partitionId, nodeId });

            return acquired;
        }

        // Renew a lock on a partition
        public async Task<bool> TryToKeepPartitionAsync(string partitionId)
        {
            var nodeId = this.clusterNodes.GetCurrentNodeId();

            this.log.Debug("Attempting to renew lock on partition...", () => new { partitionId, nodeId });
            var renewed = await this.partitionsStorage.TryToLockAsync(partitionId, nodeId, null, this.partitionLockDurationSecs);
            this.log.Debug(renewed ? "Partition lock renewed" : "Partition lock not renewed", () => new { partitionId, nodeId });

            return renewed;
        }

        // Split the full list of devices in small chunks and save them as partitions
        private async Task CreatePartitionsInternalAsync(Models.Simulation sim, Dictionary<string, List<string>> allDeviceIdsByModel)
        {
            var deviceCount = allDeviceIdsByModel.Select(x => x.Value.Count).Sum();

            this.log.Debug("Creating partitions for the simulation...",
                () => new { Simulation = sim.Id, deviceCount, this.maxPartitionSize });

            var partitionCount = 0;
            var currentSize = 0;

            // A partition contains multiple devices, organized by model ID
            var partitionContent = new Dictionary<string, List<string>>();

            foreach (KeyValuePair<string, List<string>> deviceIdsInModel in allDeviceIdsByModel)
            {
                var modelId = deviceIdsInModel.Key;

                if (!partitionContent.ContainsKey(modelId))
                {
                    partitionContent[modelId] = new List<string>();
                }

                foreach (var deviceId in deviceIdsInModel.Value)
                {
                    partitionContent[modelId].Add(deviceId);
                    currentSize++;

                    if (currentSize < this.maxPartitionSize) continue;

                    partitionCount++;
                    await this.CreatePartitionAsync(sim.Id, partitionCount, partitionContent);
                    partitionContent[modelId].Clear();
                    currentSize = 0;
                }
            }

            // Save last data
            if (currentSize > 0)
            {
                partitionCount++;
                await this.CreatePartitionAsync(sim.Id, partitionCount, partitionContent);
            }

            this.log.Debug("Partitions created",
                () => new { Simulation = sim.Id, partitionCount, this.maxPartitionSize });
        }

        private async Task CreatePartitionAsync(string simId, int partitionNumber, Dictionary<string, List<string>> deviceIdsByModel)
        {
            var partitionId = this.GetPartitionId(simId, partitionNumber);
            int partitionSize = deviceIdsByModel.Select(x => x.Value.Count).Sum();

            this.log.Debug(
                "Creating partition...",
                () => new { simId, partitionId, partitionSize });

            var partition = new DevicesPartition
            {
                Id = partitionId,
                SimulationId = simId,
                Size = partitionSize,
                DeviceIdsByModel = deviceIdsByModel
            };

            var partitionRecord = this.partitionsStorage.BuildRecord(
                partition.Id,
                JsonConvert.SerializeObject(partition));

            await this.partitionsStorage.CreateAsync(partitionRecord);

            this.log.Debug(
                "Partition created",
                () => new { simId, partitionId, partitionSize });
        }

        private async Task DeletePartitionsAsync(Models.Simulation sim, int deviceCount)
        {
            int partitionCount = (int) Math.Ceiling(deviceCount / (double) this.maxPartitionSize);
            this.log.Debug("Deleting incomplete partitions",
                () => new { SimulationId = sim.Id, this.maxPartitionSize, partitionCount });
            for (int i = 1; i <= partitionCount; i++)
            {
                await this.partitionsStorage.DeleteAsync(this.GetPartitionId(sim.Id, i));
            }
        }

        private string GetPartitionId(string simId, int partitionNumber)
        {
            return $"{simId}__{partitionNumber}";
        }
    }
}
