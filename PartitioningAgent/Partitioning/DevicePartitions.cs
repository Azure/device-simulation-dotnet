// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.PartitioningAgent.Partitioning
{
    public interface IDevicePartitions
    {
        Task UpdateDevicePartitionsAsync();
        Task DeleteOldSimulationsPartitionsAsync();
    }

    public class DevicePartitions : IDevicePartitions
    {
        // Each partition contains up to 500 devices by default (ignoring deletions)
        // Note: partitions might contain devices from multiple simulations, but are
        // not filled up once created if a new simulation appear.
        private const int PARTITION_SIZE = 500;

        // When creating partitions, assing 40 partitions (up to 20k devices) by default
        //private const int MAX_INITIAL_NODE_LOAD = 40;

        private readonly ISimulations simulations;
        private readonly IDevices devices;
        private readonly IStorageRecords partitionsStorage;
        private readonly ILogger log;

        public DevicePartitions(
            IServicesConfig config,
            ISimulations simulations,
            IDevices devices,
            ICluster cluster,
            IFactory factory,
            ILogger logger)
        {
            this.simulations = simulations;
            this.devices = devices;
            this.partitionsStorage = factory.Resolve<IStorageRecords>().Setup(config.PartitionsStorage);
            this.log = logger;
        }

        public async Task UpdateDevicePartitionsAsync()
        {
            // #4 : Load new simulations
            IList<Simulation> simulationsToPartition = await this.FindUnpartitionedSimulationsAsync();

            // #5 : Find devices for the simulations to be partitioned
            foreach (Simulation sim in simulationsToPartition)
            {
                // #5 : Find devices for the simulations to be partitioned
                var deviceIds = this.GetSimulationDevices(sim);

                // #5 : Delete partitions in case the previous partition setup crashed
                await this.DeletePartitionsAsync(sim, deviceIds);

                // #6 : Create partitions
                await this.CreatePartitionsAsync(sim, deviceIds);
                sim.PartitioningComplete = true;
                await this.simulations.UpsertAsync(sim);
            }

            // #7 : insert new devices, remove deleted devices
            // TODO
        }

        // #8 : Delete partitions of inactive simulations (free up nodes)
        public async Task DeleteOldSimulationsPartitionsAsync()
        {
            this.log.Debug("Deleting partitions of old simulations...", () => { });

            IList<Simulation> activeSimulations = await this.GetActiveSimulationsAsync();
            HashSet<string> simulationIds = activeSimulations.Select(x => x.Id).ToHashSet();

            var recordsToDelete = new List<string>();
            var partitions = await this.partitionsStorage.GetAllAsync();
            foreach (StorageRecord storageRecord in partitions)
            {
                // TODO: revisit StorageRecord structure, this ID could be a main field to avoid the expensive deserialization
                var partition = JsonConvert.DeserializeObject<DevicesPartition>(storageRecord.Data);
                if (!simulationIds.Contains(partition.SimulationId))
                {
                    recordsToDelete.Add(storageRecord.Id);
                }
            }

            if (recordsToDelete.Count == 0)
            {
                this.log.Debug("No partitions to delete", () => { });
                return;
            }

            this.log.Debug("List of partitions to delete ready", () => new { recordsToDelete.Count });
            await this.partitionsStorage.DeleteMultiAsync(recordsToDelete);
        }

        private async Task<IList<Simulation>> FindUnpartitionedSimulationsAsync()
        {
            var allSimulations = await this.GetActiveSimulationsAsync();
            this.log.Debug(allSimulations.Count + " active simulations loaded", () => { });

            var unpartitionedSimulations = allSimulations.Where(s => s.Enabled && !s.PartitioningComplete).ToList();
            this.log.Debug(unpartitionedSimulations.Count + " simulations need to be partitioned", () => { });

            return unpartitionedSimulations;
        }

        private async Task<IList<Simulation>> GetActiveSimulationsAsync()
        {
            return (await this.simulations.GetListAsync())
                .Where(s => s.Enabled).ToList();
        }

        private async Task CreatePartitionsAsync(Simulation sim, IList<string> deviceIds)
        {
            this.log.Debug(
                "Creating partitions for new simulation...",
                () => new { Simulation = sim.Id, DeviceCount = deviceIds.Count, PARTITION_SIZE });

            // Fill up one partition at a time, keep device IDs sequence order
            // TODO: use consistent hashing
            var partitionSize = 0;
            var partitionNumber = 0;
            var partitionContent = new List<string>();
            foreach (var id in deviceIds)
            {
                partitionContent.Add(id);
                if (++partitionSize == PARTITION_SIZE)
                {
                    await this.CreatePartitionAsync(sim.Id, ++partitionNumber, partitionContent);
                    partitionContent.Clear();
                    partitionSize = 0;
                }
            }

            if (partitionSize > 0)
            {
                await this.CreatePartitionAsync(sim.Id, ++partitionNumber, partitionContent);
            }

            this.log.Debug(
                "Partitions created",
                () => new { Simulation = sim.Id, DeviceCount = deviceIds.Count, partitionCount = partitionNumber });
        }

        private async Task CreatePartitionAsync(string simId, int partitionNumber, List<string> deviceIds)
        {
            var partitionId = this.GetPartitionId(simId, partitionNumber);

            this.log.Debug(
                "Creating partition...",
                () => new { simId, partitionId, DeviceCount = deviceIds.Count });

            var partition = new DevicesPartition
            {
                Id = partitionId,
                SimulationId = simId,
                Size = deviceIds.Count,
                DeviceIds = deviceIds
            };

            var partitionRecord = new StorageRecord
            {
                Id = partitionId,
                Data = JsonConvert.SerializeObject(partition)
            };

            await this.partitionsStorage.CreateAsync(partitionRecord);

            this.log.Debug(
                "Partition created",
                () => new { simId, partitionId, DeviceCount = deviceIds.Count });
        }

        private async Task DeletePartitionsAsync(Simulation sim, IList<string> deviceIds)
        {
            int partitionCount = (int) Math.Ceiling((double) deviceIds.Count / (double) PARTITION_SIZE);
            this.log.Debug("Deleting incomplete partitions", () => new { Simulation = sim.Id, PARTITION_SIZE, partitionCount });
            for (int i = 1; i <= partitionCount; i++)
            {
                await this.partitionsStorage.DeleteAsync(this.GetPartitionId(sim.Id, i));
            }
        }

        private string GetPartitionId(string simId, int partitionNumber)
        {
            return $"{simId}__{partitionNumber}";
        }

        /// <summary>
        /// Generate the list of device IDs. This list will eventually be retrieved from the database.
        /// </summary>
        private List<string> GetSimulationDevices(Simulation sim)
        {
            var result = new List<string>();
            foreach (var model in sim.DeviceModels)
            {
                for (int i = 0; i < model.Count; i++)
                {
                    var id = this.devices.GenerateId(model.Id, i);
                    result.Add(id);
                }
            }

            this.log.Debug("Device IDs loaded", () => new { Simulation = sim.Id, result.Count });

            return result;
        }
    }
}
