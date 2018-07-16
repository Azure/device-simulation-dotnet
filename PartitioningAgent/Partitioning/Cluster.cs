// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.PartitioningAgent.Partitioning
{
    public interface ICluster
    {
        Task UpsertNodeAsync(string nodeId);
        Task RemoveStaleNodesAsync();

        Task<bool> SelfElectToMasterNodeAsync(string nodeId);
        //Task<SortedSet<string>> GetSortedListAsync();
    }

    public class Cluster : ICluster
    {
        // When a node record is older than 60 seconds it's removed from the list, so the count is eventually consistent
        private const int NODE_RECORD_MAX_AGE_SECS = 60;

        // if a lock is older than 2 minutes, then it's expired
        private const int LOCK_MAX_AGE_SECS = 120;

        // Master node record id
        private const string MASTER_NODE_KEY = "MasterNode";

        private readonly IStorageRecords clusterNodes;
        private readonly IStorageRecords mainStorage;
        private readonly ILogger log;

        public Cluster(
            IServicesConfig config,
            IFactory factory,
            ILogger logger)
        {
            this.clusterNodes = factory.Resolve<IStorageRecords>().Setup(config.NodesStorage);
            this.mainStorage = factory.Resolve<IStorageRecords>().Setup(config.MainStorage);
            this.log = logger;
        }

        // Create or Update the cluster node record
        public async Task UpsertNodeAsync(string nodeId)
        {
            try
            {
                this.log.Debug("Getting cluster node record", () => { });
                StorageRecord node = await this.clusterNodes.GetAsync(nodeId);
                node.ExpiresInSecs(NODE_RECORD_MAX_AGE_SECS);
                await this.clusterNodes.UpsertAsync(node);
            }
            catch (ResourceNotFoundException)
            {
                this.log.Info("Cluster node record not found, will create it", () => new { nodeId });
                await this.InsertNodeAsync(nodeId);
            }
        }

        // Delete old node records, so that the count of nodes is eventually consistent.
        // Keeping the count correct is important, so that each node will adjust the speed
        // accordingly to IoT Hub quota, trying to avoid throttling.
        public async Task RemoveStaleNodesAsync()
        {
            try
            {
                // GetAllAsync internally deletes expired records
                await this.clusterNodes.GetAllAsync();
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while purging expired nodes", () => new { e });
            }
        }

        // Try to elect the current node to master node. Master node is responsible for
        // assigning devices to individual nodes, in order to distribute the load across
        // multiple VMs.
        public async Task<bool> SelfElectToMasterNodeAsync(string nodeId)
        {
            try
            {
                if (!await this.mainStorage.ExistsAsync(MASTER_NODE_KEY))
                {
                    this.log.Debug(
                        "The key to lock the master role doesn't exist yet, will create",
                        () => new { nodeId, MASTER_NODE_KEY });

                    var record = new StorageRecord { Id = MASTER_NODE_KEY, Data = nodeId };
                    await this.mainStorage.CreateAsync(record);
                }

                var acquired = await this.mainStorage.TryToLockAsync(MASTER_NODE_KEY, nodeId, this, LOCK_MAX_AGE_SECS);
                this.log.Debug(acquired ? "Master role acquired" : "Master role not acquired", () => new { nodeId, LOCK_MAX_AGE_SECS });
                return acquired;
            }
            catch (ConflictingResourceException)
            {
                this.log.Info("Some other node became master, nothing to do", () => new { nodeId });
                return false;
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while fetching data from the main storage", () => new { MASTER_NODE_KEY, e });
                return false;
            }
        }

        // public async Task<SortedSet<string>> GetSortedListAsync()
        // {
        //     var nodeRecords = await this.clusterNodes.GetAllAsync();
        //     var result = new SortedSet<string>();
        //     foreach (var nodeRecord in nodeRecords)
        //     {
        //         result.Add(nodeRecord.Id);
        //     }
        //
        //     return result;
        // }

        // Insert a node in the list of nodes
        private async Task InsertNodeAsync(string nodeId)
        {
            var node = new StorageRecord { Id = nodeId };
            node.ExpiresInSecs(NODE_RECORD_MAX_AGE_SECS);

            try
            {
                // If this throws an exception, the application will retry later
                await this.clusterNodes.CreateAsync(node);
            }
            catch (ConflictingResourceException e)
            {
                // This should never happen because the node ID is unique
                this.log.Error(
                    "The cluster node record has been created by another process",
                    () => new { nodeId, e });
            }
            catch (Exception e)
            {
                // This might happen in case of storage or network errors
                this.log.Error("Failed to upsert cluster node record", () => new { nodeId, e });
            }
        }

        // // Call this method when the current node:
        // //  1. is already a master, and wants to keep the role
        // //  2. or, is not the master, but the previous master hasn't renewed its lock, probably because it crashed.
        // // Returns true only if successfully gets or renew the role of master.
        // private async Task<bool> TryToUpdateMasterNodeRecordAsync(string nodeId, long now, StorageRecord nodeRecord, MasterNodeData nodeData)
        // {
        //     try
        //     {
        //         nodeData.NodeId = nodeId;
        //         nodeData.ExpiresUnixTimeSecs = now + LOCK_MAX_AGE_SECS;
        //         nodeRecord.Data = JsonConvert.SerializeObject(nodeData);
        //         await this.mainStorage.UpsertAsync(MASTER_NODE_KEY, nodeRecord);
        //         return true;
        //     }
        //     catch (ConflictingResourceException)
        //     {
        //         this.log.Info("Some other node got elected to master, nothing to do", () => new { nodeId });
        //         return false;
        //     }
        //     catch (Exception e)
        //     {
        //         this.log.Error("Unexpected error while attempting to get or renew master role", () => new { nodeId, e });
        //         return false;
        //     }
        // }
    }
}
