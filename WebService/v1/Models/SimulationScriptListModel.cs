// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class SimulationScriptListModel
    {
        [JsonProperty(PropertyName = "Items")]
        public List<SimulationScriptApiModel.SimulationScriptApiModel> Items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "SimulationScriptList;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/simulationscripts" }
        };

        public SimulationScriptListModel()
        {
            this.Items = new List<SimulationScriptApiModel.SimulationScriptApiModel>();
        }

        // Map service model to API model
        public static SimulationScriptListModel FromServiceModel(IEnumerable<SimulationScript> value)
        {
            if (value == null) return null;

            return new SimulationScriptListModel
            {
                Items = value.Select(SimulationScriptApiModel.SimulationScriptApiModel.FromServiceModel)
                    .Where(x => x != null)
                    .ToList()
            };
        }
    }
}
