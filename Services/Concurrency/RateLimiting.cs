// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

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
        void ResetCounters();
        void SetCounters(Models.Simulation simulation, ILogger log);
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
            log.Info("Rate limiting started. This message should appear only once in the logs.", () => { });
        }

        public void SetCounters(
                Services.Models.Simulation simulation,
                ILogger log)
        {
            if (simulation.ConnectionsPerSecond > 0)
            {
              this.connections = new PerSecondCounter(simulation.ConnectionsPerSecond, "Device connections", log);
            }

            if (simulation.RegistryOperationsPerMinute > 0)
            {
              this.registryOperations = new PerMinuteCounter(simulation.RegistryOperationsPerMinute, "Registry operations", log);
            }

            if (simulation.TwinReadsPerSecond > 0)
            {
              this.twinReads = new PerSecondCounter(simulation.TwinReadsPerSecond, "Twin reads", log);
            }

            if (simulation.TwinWritesPerSecond > 0)
            {

              this.twinWrites = new PerSecondCounter(simulation.TwinWritesPerSecond, "Twin writes", log);
            }

            if (simulation.DeviceMessagesPerSecond > 0)
            {
              this.messaging = new PerSecondCounter(simulation.DeviceMessagesPerSecond, "Device msg/sec", log);
            }

            log.Info("Rate limiting started. This message should appear only once in the logs.", () => { });
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
        public double GetThroughputForMessages() => this.messaging.GetThroughputForMessages();
    }
}
