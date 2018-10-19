// Copyright (c) Microsoft. All rights reserved.

using Jint.Parser.Ast;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class SimulationRateLimits
    {
        [JsonProperty(PropertyName = "ConnectionsPerSecond")]
        public int ConnectionsPerSecond { get; set; }

        [JsonProperty(PropertyName = "RegistryOperationsPerMinute")]
        public int RegistryOperationsPerMinute { get; set; }

        [JsonProperty(PropertyName = "TwinReadsPerSecond")]
        public int TwinReadsPerSecond { get; set; }

        [JsonProperty(PropertyName = "TwinWritesPerSecond")]
        public int TwinWritesPerSecond { get; set; }

        [JsonProperty(PropertyName = "DeviceMessagesPerSecond")]
        public int DeviceMessagesPerSecond { get; set; }

        [JsonProperty(PropertyName = "DeviceMessagesPerDay")]
        public long DeviceMessagesPerDay { get; set; }

        // Default constructor used by web service requests
        public SimulationRateLimits()
        {
            this.ConnectionsPerSecond = 0;
            this.RegistryOperationsPerMinute = 0;
            this.TwinReadsPerSecond = 0;
            this.TwinWritesPerSecond = 0;
            this.DeviceMessagesPerSecond = 0;
            this.DeviceMessagesPerDay = 0;
        }

        // Map API model to service model
        public static SimulationRateLimits FromServiceModel(Services.Models.Simulation.SimulationRateLimits simulationRateLimits)
        {
            return new SimulationRateLimits
            {
                ConnectionsPerSecond = simulationRateLimits.ConnectionsPerSecond,
                RegistryOperationsPerMinute = simulationRateLimits.RegistryOperationsPerMinute,
                TwinReadsPerSecond = simulationRateLimits.TwinReadsPerSecond,
                TwinWritesPerSecond = simulationRateLimits.TwinWritesPerSecond,
                DeviceMessagesPerSecond = simulationRateLimits.DeviceMessagesPerSecond,
                DeviceMessagesPerDay = simulationRateLimits.DeviceMessagesPerDay
            };
        }

        // Map API model to service model
        public Services.Models.Simulation.SimulationRateLimits ToServiceModel(IRateLimitingConfig defaultRateLimits)
        {
            var connectionsPerSecond = this.ConnectionsPerSecond > 0 ? this.ConnectionsPerSecond : defaultRateLimits.ConnectionsPerSecond;
            var registryOperationsPerMinute = this.RegistryOperationsPerMinute > 0 ? this.RegistryOperationsPerMinute : defaultRateLimits.RegistryOperationsPerMinute;
            var twinReadsPerSecond = this.TwinReadsPerSecond > 0 ? this.TwinReadsPerSecond : defaultRateLimits.TwinReadsPerSecond;
            var twinWritesPerSecond = this.TwinWritesPerSecond > 0 ? this.TwinWritesPerSecond : defaultRateLimits.TwinWritesPerSecond;
            var deviceMessagesPerSecond = this.DeviceMessagesPerSecond > 0 ? this.DeviceMessagesPerSecond : defaultRateLimits.DeviceMessagesPerSecond;
            var deviceMessagesPerDay = this.DeviceMessagesPerDay > 0 ? this.DeviceMessagesPerDay : defaultRateLimits.DeviceMessagesPerDay;

            return new Services.Models.Simulation.SimulationRateLimits
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
