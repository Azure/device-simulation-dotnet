// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    public interface IRateLimitingConfiguration
    {
        int RegistryOperationsPerMinute { get; set; }
        int TwinReadsPerSecond { get; set; }
        int TwinWritesPerSecond { get; set; }
        int ConnectionsPerSecond { get; set; }
        int DeviceMessagesPerSecond { get; set; }
        int DeviceMessagesPerDay { get; set; }
    }

    public class RateLimitingConfiguration : IRateLimitingConfiguration
    {
        public int RegistryOperationsPerMinute { get; set; }
        public int TwinReadsPerSecond { get; set; }
        public int TwinWritesPerSecond { get; set; }
        public int ConnectionsPerSecond { get; set; }
        public int DeviceMessagesPerSecond { get; set; }
        public int DeviceMessagesPerDay { get; set; }
    }
}
