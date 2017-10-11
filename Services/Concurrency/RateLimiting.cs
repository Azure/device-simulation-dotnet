// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public interface IRateLimiting
    {
        //Task<bool> LimitRegistryOperationsAsync();
        //Task<bool> LimitTwinReadOperationsAsync();
        //Task<bool> LimitTwinWriteOperationsAsync();

        Task<T> LimitRegistryOperationsAsync<T>(Func<Task<T>> func);
        Task<T> LimitTwinReadOperationsAsync<T>(Func<Task<T>> func);
        Task<T> LimitTwinWriteOperationsAsync<T>(Func<Task<T>> func);
    }

    public class RateLimiting : IRateLimiting
    {
        private static string instanceId = "";

        // Use separate objects to reduce contentions
        private readonly PerMinuteCounter registryOperations;

        private readonly PerSecondCounter twinReadOperations;
        private readonly PerSecondCounter twinWriteOperations;

        public RateLimiting(IRateLimitingConfiguration config)
        {
            MustBeSingleton();
            this.registryOperations = new PerMinuteCounter(config.RegistryOperationsPerMinute);
            this.twinReadOperations = new PerSecondCounter(config.TwinReadsPerSecond);
            this.twinWriteOperations = new PerSecondCounter(config.TwinWritesPerSecond);
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

        private static void MustBeSingleton()
        {
            CheckSingleton();
            lock (instanceId)
            {
                CheckSingleton();
                instanceId = Guid.NewGuid().ToString();
            }
        }

        private static void CheckSingleton()
        {
            if (!string.IsNullOrEmpty(instanceId))
            {
                throw new ConcurrencyException("Only one instance of this class can be instantiated.");
            }
        }
    }
}
