// Copyright (c) Microsoft. All rights reserved.

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
            return iotHub != null && iotHub.ConnectionString != USE_DEFAULT_IOTHUB
                ? iotHub.ConnectionString
                : ServicesConfig.USE_DEFAULT_IOTHUB;
        }
    }
}