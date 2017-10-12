// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public interface IRateLimiting
    {
        Task<T> LimitRegistryOperationsAsync<T>(Func<Task<T>> func);
        Task<T> LimitTwinReadOperationsAsync<T>(Func<Task<T>> func);
        Task<T> LimitTwinWriteOperationsAsync<T>(Func<Task<T>> func);
    }

    public class RateLimiting : IRateLimiting
    {
        // Use separate objects to reduce internal contentions on the lock statement

        private readonly PerMinuteCounter registryOperations;
        private readonly PerSecondCounter twinReadOperations;
        private readonly PerSecondCounter twinWriteOperations;

        public RateLimiting(
            IRateLimitingConfiguration config,
            ILogger log)
        {
            this.registryOperations = new PerMinuteCounter(
                config.RegistryOperationsPerMinute, "Registry operations", log);

            this.twinReadOperations = new PerSecondCounter(
                config.TwinReadsPerSecond, "Twin reads", log);

            this.twinWriteOperations = new PerSecondCounter(
                config.TwinWritesPerSecond, "Twin writes", log);

            // The class should be a singleton, if this appears more than once
            // something is not setup correctly and the rating won't work.
            // TODO: enforce the single instance, compatibly with the use of
            //       Parallel.For in the simulation runner.
            log.Info("Rate limiting started. This message should appear only once in the logs.", () => { });
        }

        public async Task<T> LimitRegistryOperationsAsync<T>(Func<Task<T>> func)
        {
            await this.registryOperations.RateAsync();
            return await func.Invoke();
        }

        public async Task<T> LimitTwinReadOperationsAsync<T>(Func<Task<T>> func)
        {
            await this.twinReadOperations.RateAsync();
            return await func.Invoke();
        }

        public async Task<T> LimitTwinWriteOperationsAsync<T>(Func<Task<T>> func)
        {
            await this.twinWriteOperations.RateAsync();
            return await func.Invoke();
        }
    }
}
