// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public interface IRateLimitingConfig
    {
        int RegistryOperationsPerMinute { get; }
        int TwinReadsPerSecond { get; }
        int TwinWritesPerSecond { get; }
        int ConnectionsPerSecond { get; }
        int DeviceMessagesPerSecond { get; }
        long DeviceMessagesPerDay { get; }
        IRateLimitingConfig ToServiceModel(IRateLimitingConfig defaultRateLimits);
    }

    public class RateLimitingConfig : IRateLimitingConfig
    {
        public int RegistryOperationsPerMinute { get; set; }
        public int TwinReadsPerSecond { get; set; }
        public int TwinWritesPerSecond { get; set; }
        public int ConnectionsPerSecond { get; set; }
        public int DeviceMessagesPerSecond { get; set; }
        public long DeviceMessagesPerDay { get; set; }

        public IRateLimitingConfig ToServiceModel(IRateLimitingConfig defaultRateLimits)
        {
            var connectionsPerSecond = this.ConnectionsPerSecond > 0 ? this.ConnectionsPerSecond : defaultRateLimits.ConnectionsPerSecond;
            var registryOperationsPerMinute = this.RegistryOperationsPerMinute > 0 ? this.RegistryOperationsPerMinute : defaultRateLimits.RegistryOperationsPerMinute;
            var twinReadsPerSecond = this.TwinReadsPerSecond > 0 ? this.TwinReadsPerSecond : defaultRateLimits.TwinReadsPerSecond;
            var twinWritesPerSecond = this.TwinWritesPerSecond > 0 ? this.TwinWritesPerSecond : defaultRateLimits.TwinWritesPerSecond;
            var deviceMessagesPerSecond = this.DeviceMessagesPerSecond > 0 ? this.DeviceMessagesPerSecond : defaultRateLimits.DeviceMessagesPerSecond;
            var deviceMessagesPerDay = this.DeviceMessagesPerDay > 0 ? this.DeviceMessagesPerDay : defaultRateLimits.DeviceMessagesPerDay;

            return new RateLimitingConfig
            {
                ConnectionsPerSecond = connectionsPerSecond,
                RegistryOperationsPerMinute = registryOperationsPerMinute,
                TwinReadsPerSecond = twinReadsPerSecond,
                TwinWritesPerSecond = twinWritesPerSecond,
                DeviceMessagesPerSecond = deviceMessagesPerSecond,
                DeviceMessagesPerDay = deviceMessagesPerDay
            };
        }
    }
}
