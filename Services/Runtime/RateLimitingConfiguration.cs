// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    public interface IRateLimitingConfiguration
    {
        double RegistryOperationsPerMinute { get; set; }
        double TwinReadsPerSecond { get; set; }
        double TwinWritesPerSecond { get; set; }
        double ConnectionsPerSecond { get; set; }
        double MessagesPerDay { get; set; }
    }

    public class RateLimitingConfiguration : IRateLimitingConfiguration
    {
        public double RegistryOperationsPerMinute { get; set; }
        public double TwinReadsPerSecond { get; set; }
        public double TwinWritesPerSecond { get; set; }
        public double ConnectionsPerSecond { get; set; }
        public double MessagesPerDay { get; set; }
    }
}
