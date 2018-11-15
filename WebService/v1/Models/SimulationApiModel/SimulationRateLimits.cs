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
    }
}
