// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public interface IRateLimiting
    {
        long GetPauseForNextConnection();
        long GetPauseForNextRegistryOperation();
        long GetPauseForNextTwinRead();
        long GetPauseForNextTwinWrite();
        long GetPauseForNextMessage();
        double GetThroughputForMessages();
        Models.Simulation.SimulationRateLimits GetCounters();
        void SetCounters(Models.Simulation.SimulationRateLimits simulationRateLimits, ILogger log);
        void ResetCounters();
    }

    public class RateLimiting : IRateLimiting
    {
        // Use separate objects to reduce internal contentions in the lock statement

        private PerSecondCounter connections;
        private PerMinuteCounter registryOperations;
        private PerSecondCounter twinReads;
        private PerSecondCounter twinWrites;
        private PerSecondCounter messaging;

        // TODO: https://github.com/Azure/device-simulation-dotnet/issues/80
        //private readonly PerDayCounter messagingDaily;

        public RateLimiting(
            IRateLimitingConfig config,
            ILogger log)
        {
            this.connections = new PerSecondCounter(
                config.ConnectionsPerSecond, "Device connections", log);

            this.registryOperations = new PerMinuteCounter(
                config.RegistryOperationsPerMinute, "Registry operations", log);

            this.twinReads = new PerSecondCounter(
                config.TwinReadsPerSecond, "Twin reads", log);

            this.twinWrites = new PerSecondCounter(
                config.TwinWritesPerSecond, "Twin writes", log);

            this.messaging = new PerSecondCounter(
                config.DeviceMessagesPerSecond, "Device msg/sec", log);

            //this.messagingDaily = new PerDayCounter(
            //    config.DeviceMessagesPerDay, "Device msg/day", log);

            // The class should be a singleton, if this appears more than once
            // something is not setup correctly and the rating won't work.
            // TODO: enforce the single instance, compatibly with the use of
            //       Parallel.For in the simulation runner.
            //       https://github.com/Azure/device-simulation-dotnet/issues/79
            log.Info("Rate limiting started. This message should appear only once in the logs.");
        }

        public Models.Simulation.SimulationRateLimits GetCounters()
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

        public void SetCounters(
            Models.Simulation.SimulationRateLimits simulationRateLimits, 
            ILogger log)
        {
            if (simulationRateLimits.ConnectionsPerSecond > 0)
            {
                this.connections = new PerSecondCounter(simulationRateLimits.ConnectionsPerSecond, "Device connections", log);
            }

            if (simulationRateLimits.RegistryOperationsPerMinute > 0)
            {
                this.registryOperations = new PerMinuteCounter(simulationRateLimits.RegistryOperationsPerMinute, "Registry operations", log);
            }

            if (simulationRateLimits.TwinReadsPerSecond > 0)
            {
                this.twinReads = new PerSecondCounter(simulationRateLimits.TwinReadsPerSecond, "Twin reads", log);
            }

            if (simulationRateLimits.TwinWritesPerSecond > 0)
            {
                this.twinWrites = new PerSecondCounter(simulationRateLimits.TwinWritesPerSecond, "Twin writes", log);
            }

            if (simulationRateLimits.DeviceMessagesPerSecond > 0)
            {
                this.messaging = new PerSecondCounter(simulationRateLimits.DeviceMessagesPerSecond, "Device msg/sec", log);
            }

            log.Info("Rate limiting started. This message should appear only once in the logs.");
        }

        public void ResetCounters()
        {
            this.connections.ResetCounter();
            this.registryOperations.ResetCounter();
            this.twinReads.ResetCounter();
            this.twinWrites.ResetCounter();
            this.messaging.ResetCounter();
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
        public double GetThroughputForMessages() => System.Math.Ceiling(this.messaging.GetThroughputForMessages() * 100) / 100;
    }
}
