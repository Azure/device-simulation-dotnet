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
        Models.Simulation.SimulationRateLimits GetRateLimits();
        void SetCounters(Models.Simulation.SimulationRateLimits simulationRateLimits);
        void ResetCounters();
        void ChangeClusterSize(int currentCount);
        void Init(Models.Simulation.SimulationRateLimits rateLimits);
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
            ILogger log,
            IInstance instance)
        {
            this.log = log;
            this.clusterSize = 1;
            this.instance = instance;
        }

        public void Init(Models.Simulation.SimulationRateLimits rateLimits)
        {
            this.instance.InitOnce();

            this.connections = new PerSecondCounter(
                rateLimits.ConnectionsPerSecond, "Device connections", this.log);

            this.registryOperations = new PerMinuteCounter(
                rateLimits.RegistryOperationsPerMinute, "Registry operations", this.log);

            this.twinReads = new PerSecondCounter(
                rateLimits.TwinReadsPerSecond, "Twin reads", this.log);

            this.twinWrites = new PerSecondCounter(
                rateLimits.TwinWritesPerSecond, "Twin writes", this.log);

            this.messaging = new PerSecondCounter(
                rateLimits.DeviceMessagesPerSecond, "Device msg/sec", this.log);

            this.instance.InitComplete();
        }

        public Models.Simulation.SimulationRateLimits GetRateLimits()
        {
            return new Models.Simulation.SimulationRateLimits
            {
                ConnectionsPerSecond = this.connections.EventsPerTimeUnit,
                RegistryOperationsPerMinute = this.registryOperations.EventsPerTimeUnit,
                TwinWritesPerSecond = this.twinReads.EventsPerTimeUnit,
                TwinReadsPerSecond = this.twinWrites.EventsPerTimeUnit,
                DeviceMessagesPerSecond = this.messaging.EventsPerTimeUnit
            };
        }

        public void SetCounters(Models.Simulation.SimulationRateLimits simulationRateLimits)
        {
            if (simulationRateLimits.ConnectionsPerSecond > 0)
            {
                this.connections = new PerSecondCounter(simulationRateLimits.ConnectionsPerSecond, "Device connections", this.log);
            }

            if (simulationRateLimits.RegistryOperationsPerMinute > 0)
            {
                this.registryOperations = new PerMinuteCounter(simulationRateLimits.RegistryOperationsPerMinute, "Registry operations", this.log);
            }

            if (simulationRateLimits.TwinReadsPerSecond > 0)
            {
                this.twinReads = new PerSecondCounter(simulationRateLimits.TwinReadsPerSecond, "Twin reads", this.log);
            }

            if (simulationRateLimits.TwinWritesPerSecond > 0)
            {
                this.twinWrites = new PerSecondCounter(simulationRateLimits.TwinWritesPerSecond, "Twin writes", this.log);
            }

            if (simulationRateLimits.DeviceMessagesPerSecond > 0)
            {
                this.messaging = new PerSecondCounter(simulationRateLimits.DeviceMessagesPerSecond, "Device msg/sec", this.log);
            }

            this.log.Info("Rate limiting started. This message should appear only once in the logs.");
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
