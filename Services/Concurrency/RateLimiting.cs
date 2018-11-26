// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public interface IRateLimiting
    {
        int ClusterSize { get; }

        void Init(IRateLimitingConfig config);
        long GetPauseForNextConnection();
        long GetPauseForNextRegistryOperation();
        long GetPauseForNextTwinRead();
        long GetPauseForNextTwinWrite();
        long GetPauseForNextMessage();

        // Get message throughput (messages per second)
        double GetThroughputForMessages();

        // Change the number of VMs, which affects the rating speed
        void ChangeClusterSize(int currentCount);
    }

    // TODO: https://github.com/Azure/device-simulation-dotnet/issues/80
    public class RateLimiting : IRateLimiting
    {
        private readonly ILogger log;
        private readonly IInstance instance;

        // Cluster size, used to calculate the rate for each node
        private int clusterSize;

        // Use separate objects to reduce internal contentions in the lock statement
        private PerSecondCounter connections;
        private PerMinuteCounter registryOperations;
        private PerSecondCounter twinReads;
        private PerSecondCounter twinWrites;
        private PerSecondCounter messaging;

        public int ClusterSize => this.clusterSize;

        // Note: this class should be used only via the simulation context
        // to ensure that concurrent simulations don't share data
        public RateLimiting(
            ILogger log,
            IInstance instance)
        {
            this.log = log;
            this.clusterSize = 1;
            this.instance = instance;
        }

        public void Init(IRateLimitingConfig config)
        {
            this.instance.InitOnce();

            this.connections = new PerSecondCounter(
                config.ConnectionsPerSecond, "Device connections", this.log);

            this.registryOperations = new PerMinuteCounter(
                config.RegistryOperationsPerMinute, "Registry operations", this.log);

            this.twinReads = new PerSecondCounter(
                config.TwinReadsPerSecond, "Twin reads", this.log);

            this.twinWrites = new PerSecondCounter(
                config.TwinWritesPerSecond, "Twin writes", this.log);

            this.messaging = new PerSecondCounter(
                config.DeviceMessagesPerSecond, "Device msg/sec", this.log);

            this.instance.InitComplete();
        }

        public long GetPauseForNextConnection()
        {
            return this.connections.GetPause();
        }

        public long GetPauseForNextRegistryOperation()
        {
            return this.registryOperations.GetPause();
        }

        public long GetPauseForNextTwinRead()
        {
            return this.twinReads.GetPause();
        }

        public long GetPauseForNextTwinWrite()
        {
            return this.twinWrites.GetPause();
        }

        public long GetPauseForNextMessage()
        {
            return this.messaging.GetPause();
        }

        // Change the rating behavior to 'normal' when quota is not exceeded

        // Get message throughput (messages per second)
        public double GetThroughputForMessages()
        {
            return this.messaging.GetThroughputForMessages();
        }

        // Change the number of VMs, which affects the rating speed
        public void ChangeClusterSize(int count)
        {
            this.log.Info("Updating rating limits to the new cluster size",
                () => new { previousSize = this.clusterSize, newSize = count });

            this.clusterSize = count;
            this.connections.ChangeConcurrencyFactor(count);
            this.registryOperations.ChangeConcurrencyFactor(count);
            this.twinReads.ChangeConcurrencyFactor(count);
            this.twinWrites.ChangeConcurrencyFactor(count);
            this.messaging.ChangeConcurrencyFactor(count);
        }
    }
}
