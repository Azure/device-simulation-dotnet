// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class SimulationIotHub
    {
        // This is the value used by the user interface
        public const string USE_DEFAULT_IOTHUB = "default";

        [JsonProperty(PropertyName = "ConnectionString")]
        public string ConnectionString { get; set; }

        // Default constructor used by web service requests
        public SimulationIotHub()
        {
            this.ConnectionString = USE_DEFAULT_IOTHUB;
        }

        public SimulationIotHub(string connectionString) : this()
        {
            this.ConnectionString = connectionString;
        }

        // Map API model to service model
        public static string ToServiceModel(SimulationIotHub iotHub)
        {
            return iotHub != null && !IsDefaultHub(iotHub.ConnectionString)
                ? iotHub.ConnectionString
                : ServicesConfig.USE_DEFAULT_IOTHUB;
        }

        private static bool IsDefaultHub(string connectionString)
        {
            return
                connectionString == null ||
                connectionString == string.Empty ||
                string.Equals(
                    connectionString.Trim(),
                    USE_DEFAULT_IOTHUB,
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}