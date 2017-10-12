// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public interface IRateLimiting
    {
        Task<T> LimitConnectionsAsync<T>(Func<Task<T>> func);
        Task LimitConnectionsAsync(Func<Task> func);

        Task<T> LimitRegistryOperationsAsync<T>(Func<Task<T>> func);
        Task LimitRegistryOperationsAsync(Func<Task> func);

        Task<T> LimitTwinReadsAsync<T>(Func<Task<T>> func);
        Task LimitTwinReadsAsync(Func<Task> func);

        Task<T> LimitTwinWritesAsync<T>(Func<Task<T>> func);
        Task LimitTwinWritesAsync(Func<Task> func);

        Task<T> LimitMessagesAsync<T>(Func<Task<T>> func);
        Task LimitMessagesAsync(Func<Task> func);
    }

    public class RateLimiting : IRateLimiting
    {
        // Use separate objects to reduce internal contentions in the lock statement

        private readonly PerSecondCounter connections;
        private readonly PerMinuteCounter registryOperations;
        private readonly PerSecondCounter twinReads;
        private readonly PerSecondCounter twinWrites;
        private readonly PerDayCounter messaging;

        public RateLimiting(
            IRateLimitingConfiguration config,
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

            this.messaging = new PerDayCounter(
                config.MessagesPerDay, "Messages", log);

            // The class should be a singleton, if this appears more than once
            // something is not setup correctly and the rating won't work.
            // TODO: enforce the single instance, compatibly with the use of
            //       Parallel.For in the simulation runner.
            log.Info("Rate limiting started. This message should appear only once in the logs.", () => { });
        }

        public async Task<T> LimitConnectionsAsync<T>(Func<Task<T>> func)
        {
            await this.connections.IncreaseAsync();
            return await func.Invoke();
        }

        public async Task LimitConnectionsAsync(Func<Task> func)
        {
            await this.connections.IncreaseAsync();
            await func.Invoke();
        }

        public async Task<T> LimitRegistryOperationsAsync<T>(Func<Task<T>> func)
        {
            await this.registryOperations.IncreaseAsync();
            return await func.Invoke();
        }

        public async Task LimitRegistryOperationsAsync(Func<Task> func)
        {
            await this.registryOperations.IncreaseAsync();
            await func.Invoke();
        }

        public async Task<T> LimitTwinReadsAsync<T>(Func<Task<T>> func)
        {
            await this.twinReads.IncreaseAsync();
            return await func.Invoke();
        }

        public async Task LimitTwinReadsAsync(Func<Task> func)
        {
            await this.twinReads.IncreaseAsync();
            await func.Invoke();
        }

        public async Task<T> LimitTwinWritesAsync<T>(Func<Task<T>> func)
        {
            await this.twinWrites.IncreaseAsync();
            return await func.Invoke();
        }

        public async Task LimitTwinWritesAsync(Func<Task> func)
        {
            await this.twinWrites.IncreaseAsync();
            await func.Invoke();
        }

        public async Task<T> LimitMessagesAsync<T>(Func<Task<T>> func)
        {
            await this.messaging.IncreaseAsync();
            return await func.Invoke();
        }

        public async Task LimitMessagesAsync(Func<Task> func)
        {
            await this.messaging.IncreaseAsync();
            await func.Invoke();
        }
    }
}
