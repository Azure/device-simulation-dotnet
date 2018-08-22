// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class SimulationStatisticsRef
    {
        [JsonProperty(PropertyName = "TotalMessagesSent", NullValueHandling = NullValueHandling.Ignore)]
        public int TotalMessagesSent { get; set; }

        [JsonProperty(PropertyName = "AverageMessagesPerSecond", NullValueHandling = NullValueHandling.Ignore)]
        public double AverageMessagesPerSecond { get; set; }

        // Default constructor used by web service requests
        public SimulationStatisticsRef()
        {
            this.TotalMessagesSent = 0;
            this.AverageMessagesPerSecond = 0;
        }

        // Map API model to service model
        public static Services.Models.Simulation.StatisticsRef ToServiceModel(SimulationStatisticsRef statistics)
        {
            if(statistics != null)
            {
                return new Services.Models.Simulation.StatisticsRef {
                    TotalMessagesSent = statistics.TotalMessagesSent,
                    AverageMessagesPerSecond = statistics.AverageMessagesPerSecond
                };
            }

            return null;
        }

        // Map API model to service model
        public static SimulationStatisticsRef FromServiceModel(Services.Models.Simulation.StatisticsRef statistics)
        {
            if (statistics != null)
            {
                return new SimulationStatisticsRef
                {
                    TotalMessagesSent = statistics.TotalMessagesSent,
                    AverageMessagesPerSecond = statistics.AverageMessagesPerSecond
                };
            }

            return null;
        }
    }
}
