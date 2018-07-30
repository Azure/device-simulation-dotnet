// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.ClusteringAgent
{
    public interface IClusteringAgent
    {
        Task StartAsync();
        void Stop();
    }

    public class Agent : IClusteringAgent
    {
        private readonly ICluster cluster;
        private readonly IDevicePartitions partitions;
        private readonly ILogger log;
        private readonly IThreadWrapper thread;
        private readonly int checkIntervalMsecs;
        private bool running;

        public Agent(
            ICluster cluster,
            IDevicePartitions partitions,
            IThreadWrapper thread,
            IClusteringConfig clusteringConfig,
            ILogger logger)
        {
            this.cluster = cluster;
            this.partitions = partitions;
            this.thread = thread;
            this.log = logger;
            this.running = true;
            this.checkIntervalMsecs = clusteringConfig.CheckIntervalMsecs;
        }

        public async Task StartAsync()
        {
            this.log.Info("Partitioning Agent running", () => new { Node = this.cluster.GetCurrentNodeId() });

            // Repeat until the agent is stopped
            while (this.running)
            {
                await this.cluster.KeepAliveNodeAsync(); // #1

                var isMaster = await this.cluster.SelfElectToMasterNodeAsync(); // #2
                if (isMaster)
                {
                    await Task.WhenAll(
                        this.cluster.RemoveStaleNodesAsync(), // #3
                        this.UpdateDevicePartitionsAsync()); // #4 #5 #6 #7 #8
                }

                this.thread.Sleep(this.checkIntervalMsecs);
            }
        }

        public void Stop()
        {
            this.running = false;
        }

        private async Task UpdateDevicePartitionsAsync()
        {
            this.log.Debug("Updating device partitions...");

            // #4 #5 #6 #7
            await this.partitions.UpdateDevicePartitionsAsync();

            // #8
            // TODO: partitions should be deleted only after its actors are down
            await this.partitions.DeleteOldSimulationsPartitionsAsync();

            this.log.Debug("Device partitions updated");
        }
    }
}
