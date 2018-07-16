// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.PartitioningAgent.Partitioning;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.PartitioningAgent
{
    public interface IPartitioningAgent
    {
        Task RunAsync();
        void Stop();
    }

    public class Agent : IPartitioningAgent
    {
        // Every 15 seconds update the list of nodes
        // and update the partitions
        private const int CHECK_INTERVAL_MSECS = 15000;

        private readonly ICluster cluster;
        private readonly IDevicePartitions partitions;
        private readonly ILogger log;
        private readonly IThreadWrapper thread;
        private readonly string nodeId;
        private bool running;

        public Agent(
            ICluster cluster,
            IDevicePartitions partitions,
            IThreadWrapper thread,
            ILogger logger)
        {
            this.cluster = cluster;
            this.partitions = partitions;
            this.thread = thread;
            this.log = logger;
            this.nodeId = GenerateNodeId();
            this.running = true;
        }

        public async Task RunAsync()
        {
            this.log.Info("Partitioning Agent running", () => new { this.nodeId });

            // Repeat until the agent is stopped
            while (this.running)
            {
                await this.KeepAliveAsync(); // #1

                var isMaster = await this.IsMasterNodeAsync(); // #2
                if (isMaster)
                {
                    await Task.WhenAll(
                        this.RemoveUnresponsiveNodesAsync(), // #3
                        this.UpdateDevicePartitionsAsync()); // #4 #5 #6 #7 #8
                }

                this.thread.Sleep(CHECK_INTERVAL_MSECS);
            }
        }

        public void Stop()
        {
            this.running = false;
        }

        private async Task<bool> IsMasterNodeAsync()
        {
            this.log.Debug("Trying to acquire master role", () => new { this.nodeId });
            return await this.cluster.SelfElectToMasterNodeAsync(this.nodeId);
        }

        private async Task KeepAliveAsync()
        {
            this.log.Debug("Keeping node alive...", () => new { this.nodeId });
            await this.cluster.UpsertNodeAsync(this.nodeId);
            this.log.Debug("Node keepalive complete", () => new { this.nodeId });
        }

        private async Task RemoveUnresponsiveNodesAsync()
        {
            this.log.Debug("Removing unresponsive nodes...", () => new { this.nodeId });
            await this.cluster.RemoveStaleNodesAsync();
            this.log.Debug("Unresponsive nodes removed", () => new { this.nodeId });
        }

        private async Task UpdateDevicePartitionsAsync()
        {
            this.log.Debug("Updating device partitions...", () => new { this.nodeId });

            // #4 #5 #6 #7
            await this.partitions.UpdateDevicePartitionsAsync();

            // #8
            await this.partitions.DeleteOldSimulationsPartitionsAsync();

            this.log.Debug("Device partitions updated", () => new { this.nodeId });
        }

        // Generate a unique value used to identify the current instance
        private static string GenerateNodeId()
        {
            // Example: 12a34b5678901c23de4c5f6ab78c9012.2018-01-12T01:15:00
            return Guid.NewGuid().ToString("N") + "." + DateTimeOffset.UtcNow.ToString("s");
        }
    }
}
