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
        int DeviceMessagesPerDay { get; }
    }

    public class RateLimitingConfig : IRateLimitingConfig
    {
        public int RegistryOperationsPerMinute { get; set; }
        public int TwinReadsPerSecond { get; set; }
        public int TwinWritesPerSecond { get; set; }
        public int ConnectionsPerSecond { get; set; }
        public int DeviceMessagesPerSecond { get; set; }
        public int DeviceMessagesPerDay { get; set; }
    }
}
