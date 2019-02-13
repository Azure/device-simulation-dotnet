// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering
{
    public interface IClusterNodes
    {
        string GetCurrentNodeId();
        Task KeepAliveNodeAsync();
        Task<bool> SelfElectToMasterNodeAsync();
        Task RemoveStaleNodesAsync();
        Task<SortedSet<string>> GetSortedIdListAsync();
    }

    public class ClusterNodes : IClusterNodes
    {
        // Master node record id written and locked by the master node
        public const string MASTER_NODE_KEY = "MasterNode";

        // Generate a node id when the class is loaded. The value is shared across threads in the process.
        private static readonly string currentProcessNodeId = GenerateSharedNodeId();

        private readonly ILogger log;
        private readonly IEngine clusterNodesStorage;
        private readonly IEngine mainStorage;
        private readonly int nodeRecordMaxAgeSecs;
        private readonly int masterLockMaxAgeSecs;

        public ClusterNodes(
            IServicesConfig config,
            IClusteringConfig clusteringConfig,
            IEngines engines,
            ILogger logger)
        {
            this.log = logger;

            this.clusterNodesStorage = engines.Build(config.NodesStorage);
            this.mainStorage = engines.Build(config.MainStorage);
            this.nodeRecordMaxAgeSecs = clusteringConfig.NodeRecordMaxAgeSecs;
            this.masterLockMaxAgeSecs = clusteringConfig.MasterLockDurationSecs;
        }

        public string GetCurrentNodeId()
        {
            return currentProcessNodeId;
        }

        public async Task KeepAliveNodeAsync()
        {
            this.log.Debug("Keeping node alive...", () => new { currentProcessNodeId });

            try
            {
                this.log.Debug("Getting cluster node record");
                IDataRecord node = await this.clusterNodesStorage.GetAsync(currentProcessNodeId);
                node.ExpiresInSecs(this.nodeRecordMaxAgeSecs);
                await this.clusterNodesStorage.UpsertAsync(node);
            }
            catch (ResourceNotFoundException)
            {
                this.log.Info("Cluster node record not found, will create it", () => new { currentProcessNodeId });
                await this.InsertNodeAsync(currentProcessNodeId);
            }

            this.log.Debug("Node keep-alive complete", () => new { currentProcessNodeId });
        }

        // Try to elect the current node to master node. Master node is responsible for
        // assigning devices to individual nodes, in order to distribute the load across
        // multiple VMs.
        public async Task<bool> SelfElectToMasterNodeAsync()
        {
            this.log.Debug("Trying to acquire master role", () => new { currentProcessNodeId });

            try
            {
                if (!await this.mainStorage.ExistsAsync(MASTER_NODE_KEY))
                {
                    this.log.Debug(
                        "The key to lock the master role doesn't exist yet, will create",
                        () => new { currentProcessNodeId, MASTER_NODE_KEY });

                    var record = this.mainStorage.BuildRecord(MASTER_NODE_KEY, "Record locked by the master node");
                    await this.mainStorage.CreateAsync(record);
                }

                var acquired = await this.mainStorage.TryToLockAsync(MASTER_NODE_KEY, currentProcessNodeId, this.GetType().FullName, this.masterLockMaxAgeSecs);
                this.log.Debug(acquired ? "Master role acquired" : "Master role not acquired", 
                    () => new
                    {
                        currentProcessNodeId,
                        this.masterLockMaxAgeSecs
                    });
                return acquired;
            }
            catch (ConflictingResourceException)
            {
                this.log.Info("Some other node became master, nothing to do", () => new { currentProcessNodeId });
                return false;
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while fetching data from the main storage", () => new { MASTER_NODE_KEY, e });
                return false;
            }
        }

        // Delete old node records, so that the count of nodes is eventually consistent.
        // Keeping the count correct is important, so that each node will adjust the speed
        // accordingly to IoT Hub quota, trying to avoid throttling.
        public async Task RemoveStaleNodesAsync()
        {
            this.log.Debug("Removing unresponsive nodes...");

            try
            {
                // GetAllAsync internally deletes expired records
                await this.clusterNodesStorage.GetAllAsync();
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while purging expired nodes", e);
            }
        }

        public async Task<SortedSet<string>> GetSortedIdListAsync()
        {
            var nodeRecords = await this.clusterNodesStorage.GetAllAsync();
            var result = new SortedSet<string>();
            foreach (var nodeRecord in nodeRecords)
            {
                result.Add(nodeRecord.GetId());
            }

            return result;
        }

        // Insert a node in the list of nodes
        private async Task InsertNodeAsync(string nodeId)
        {
            var node = this.clusterNodesStorage.BuildRecord(nodeId);
            node.ExpiresInSecs(this.nodeRecordMaxAgeSecs);

            try
            {
                // If this throws an exception, the application will retry later
                await this.clusterNodesStorage.CreateAsync(node);
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

        // Generate a unique value used to identify the current instance
        private static string GenerateSharedNodeId()
        {
            // Example: 12a34b5678901c23de4c5f6ab78c9012.2018-01-12T01:15:00
            return Guid.NewGuid().ToString("N") + "." + DateTimeOffset.UtcNow.ToString("s");
        }
    }
}
