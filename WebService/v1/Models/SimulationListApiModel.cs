// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class SimulationListApiModel
    {
        [JsonProperty(PropertyName = "Items")]
        public List<SimulationApiModel.SimulationApiModel> Items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "SimulationList;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/simulations" }
        };

        public SimulationListApiModel()
        {
            this.Items = new List<SimulationApiModel.SimulationApiModel>();
        }
    }
}
