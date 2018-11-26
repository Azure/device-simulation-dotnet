// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class SimulationRateLimits 
    {
        [JsonProperty(PropertyName = "RegistryOperationsPerMinute")]
        public int RegistryOperationsPerMinute { get; set; }

        [JsonProperty(PropertyName = "TwinReadsPerSecond")]
        public int TwinReadsPerSecond { get; set; }

        [JsonProperty(PropertyName = "TwinWritesPerSecond")]
        public int TwinWritesPerSecond { get; set; }

        [JsonProperty(PropertyName = "ConnectionsPerSecond")]
        public int ConnectionsPerSecond { get; set; }

        [JsonProperty(PropertyName = "DeviceMessagesPerSecond")]
        public int DeviceMessagesPerSecond { get; set; }

        public IRateLimitingConfig ToServiceModel(IRateLimitingConfig defaultRateLimits)
        {
            var connectionsPerSecond = this.ConnectionsPerSecond > 0 ? this.ConnectionsPerSecond : defaultRateLimits.ConnectionsPerSecond;
            var registryOperationsPerMinute = this.RegistryOperationsPerMinute > 0 ? this.RegistryOperationsPerMinute : defaultRateLimits.RegistryOperationsPerMinute;
            var twinReadsPerSecond = this.TwinReadsPerSecond > 0 ? this.TwinReadsPerSecond : defaultRateLimits.TwinReadsPerSecond;
            var twinWritesPerSecond = this.TwinWritesPerSecond > 0 ? this.TwinWritesPerSecond : defaultRateLimits.TwinWritesPerSecond;
            var deviceMessagesPerSecond = this.DeviceMessagesPerSecond > 0 ? this.DeviceMessagesPerSecond : defaultRateLimits.DeviceMessagesPerSecond;

            return new RateLimitingConfig
            {
                ConnectionsPerSecond = connectionsPerSecond,
                RegistryOperationsPerMinute = registryOperationsPerMinute,
                TwinReadsPerSecond = twinReadsPerSecond,
                TwinWritesPerSecond = twinWritesPerSecond,
                DeviceMessagesPerSecond = deviceMessagesPerSecond
            };
        }

        public static SimulationRateLimits FromServiceModel(IRateLimitingConfig simulationRateLimits)
        {
            return new SimulationRateLimits
            {
                ConnectionsPerSecond = simulationRateLimits.ConnectionsPerSecond,
                RegistryOperationsPerMinute = simulationRateLimits.RegistryOperationsPerMinute,
                TwinReadsPerSecond = simulationRateLimits.TwinReadsPerSecond,
                TwinWritesPerSecond = simulationRateLimits.TwinWritesPerSecond,
                DeviceMessagesPerSecond = simulationRateLimits.DeviceMessagesPerSecond
            };
        }
    }
}
