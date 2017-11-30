// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel
{
    public class DeviceModelSimulation
    {
        [JsonProperty(PropertyName = "InitialState")]
        public Dictionary<string, object> Initial { get; set; }

        [JsonProperty(PropertyName = "Interval", NullValueHandling = NullValueHandling.Ignore)]
        public string Interval { get; set; }

        [JsonProperty(PropertyName = "Scripts")]
        public List<DeviceModelSimulationScript> SimulationScripts { get; set; }

        public DeviceModelSimulation()
        {
            this.Initial = new Dictionary<string, object>();
            this.Interval = null;
            this.SimulationScripts = new List<DeviceModelSimulationScript>();
        }

        // Map service model to API model
        public static DeviceModelSimulation FromServiceModel(DeviceModel.StateSimulation value)
        {
            if (value == null) return null;

            return new DeviceModelSimulation
            {
                Initial = value.InitialState,
                Interval = value.Interval.ToString("c"),
                SimulationScripts = value.Scripts.Select(DeviceModelSimulationScript.FromServiceModel).Where(x => x != null).ToList()
            };
        }
    }
}
