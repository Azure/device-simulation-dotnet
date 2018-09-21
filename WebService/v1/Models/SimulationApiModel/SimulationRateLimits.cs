// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
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

        // Default constructor used by web service requests
        public SimulationRateLimits()
        {
            this.ConnectionsPerSecond = 0;
            this.RegistryOperationsPerMinute = 0;
            this.TwinReadsPerSecond = 0;
            this.TwinWritesPerSecond = 0;
            this.DeviceMessagesPerSecond = 0;
        }

        // Map API model to service model
        public static SimulationRateLimits FromServiceModel(
            Services.Models.Simulation.SimulationRateLimits simulationRateLimits,
            IRateLimiting rateReporter)
        {
            var defaultRatingLimits = rateReporter.GetRateLimits();

            var connectionsPerSecond = simulationRateLimits?.ConnectionsPerSecond > 0 ? simulationRateLimits.ConnectionsPerSecond : defaultRatingLimits.ConnectionsPerSecond;
            var registryOperationsPerMinute = simulationRateLimits?.RegistryOperationsPerMinute > 0 ? simulationRateLimits.RegistryOperationsPerMinute : defaultRatingLimits.RegistryOperationsPerMinute;
            var twinReadsPerSecond = simulationRateLimits?.TwinReadsPerSecond > 0 ? simulationRateLimits.TwinReadsPerSecond : defaultRatingLimits.TwinReadsPerSecond;
            var twinWritesPerSecond = simulationRateLimits?.TwinWritesPerSecond > 0 ? simulationRateLimits.TwinWritesPerSecond : defaultRatingLimits.TwinWritesPerSecond;
            var deviceMessagesPerSecond = simulationRateLimits?.DeviceMessagesPerSecond > 0 ? simulationRateLimits.DeviceMessagesPerSecond : defaultRatingLimits.DeviceMessagesPerSecond;

            return new SimulationRateLimits
            {
                ConnectionsPerSecond = connectionsPerSecond,
                RegistryOperationsPerMinute = registryOperationsPerMinute,
                TwinReadsPerSecond = twinReadsPerSecond,
                TwinWritesPerSecond = twinWritesPerSecond,
                DeviceMessagesPerSecond = deviceMessagesPerSecond
            };
        }

        // Map API model to service model
        public Services.Models.Simulation.SimulationRateLimits ToServiceModel()
        {
            return new Services.Models.Simulation.SimulationRateLimits
            {
                ConnectionsPerSecond = this.ConnectionsPerSecond,
                RegistryOperationsPerMinute = this.RegistryOperationsPerMinute,
                TwinReadsPerSecond = this.TwinReadsPerSecond,
                TwinWritesPerSecond = this.TwinWritesPerSecond,
                DeviceMessagesPerSecond = this.DeviceMessagesPerSecond
            };
        }
    }
}
