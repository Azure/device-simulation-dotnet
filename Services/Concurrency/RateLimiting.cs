// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public interface IRateLimiting
    {
        int ClusterSize { get; }
        long GetPauseForNextConnection();
        long GetPauseForNextRegistryOperation();
        long GetPauseForNextTwinRead();
        long GetPauseForNextTwinWrite();
        long GetPauseForNextMessage();
        double GetThroughputForMessages();
        void ResetCounters();
        void ChangeClusterSize(int currentCount);
        void Init(IRateLimitingConfig rateLimitingConfig);
    }

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

        // TODO: https://github.com/Azure/device-simulation-dotnet/issues/80
        //private readonly PerDayCounter messagingDaily;
        public int ClusterSize => this.clusterSize;

        // Note: this class should be used only via the simulation context
        // to ensure that concurrent simulations don't share data
        public RateLimiting(
            IRateLimitingConfig config,
            ILogger log,
            IInstance instance)
        {
            this.log = log;
            this.clusterSize = 1;
            this.instance = instance;

            // TODO: these initializations will be moved to Init when SimulationManager is
            //       integrated.
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

        public void ResetCounters()
        {
            this.connections.ResetCounter();
            this.registryOperations.ResetCounter();
            this.twinReads.ResetCounter();
            this.twinWrites.ResetCounter();
            this.messaging.ResetCounter();
        }

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
            // TODO: consider daily quota
            // https://github.com/Azure/device-simulation-dotnet/issues/80
            return this.messaging.GetPause();
        }

        /// <summary>
        /// Get message throughput (messages per second)
        /// </summary>
        public double GetThroughputForMessages()
        {
            return this.messaging.GetThroughputForMessages();
        }
    }
}
