// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class SimulationPatchApiModel
    {
        [JsonProperty(PropertyName = "ETag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "Enabled")]
        public bool? Enabled { get; set; }

        [JsonProperty(PropertyName = "Statistics", NullValueHandling = NullValueHandling.Ignore)]
        public SimulationStatisticsRef Statistics { get; set; }

        public SimulationPatchApiModel()
        {
            this.Enabled = null;
        }

        /// <summary>Map an API model to the corresponding service model</summary>
        public Services.Models.SimulationPatch ToServiceModel(string id)
        {
            return new Services.Models.SimulationPatch
            {
                ETag = this.ETag,
                Id = id,
                Enabled = this.Enabled,
                Statistics = SimulationStatisticsRef.ToServiceModel(this.Statistics)
            };
        }
    }

    public class SimulationStatisticsRef
    {
        [JsonProperty(PropertyName = "AverageMessagesPerSecond", NullValueHandling = NullValueHandling.Ignore)]
        public double AverageMessagesPerSecond { get; set; }

        [JsonProperty(PropertyName = "TotalMessagesSent", NullValueHandling = NullValueHandling.Ignore)]
        public int TotalMessagesSent { get; set; }

        public static Services.Models.SimulationStatistics ToServiceModel(SimulationStatisticsRef statistics)
        {
            if (statistics != null)
            {
                return new Services.Models.SimulationStatistics
                {
                    AverageMessagesPerSecond = statistics.AverageMessagesPerSecond,
                    TotalMessagesSent = statistics.TotalMessagesSent
                };
            }

            return null;
        }
    }

}
