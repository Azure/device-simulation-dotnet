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

        /// <summary>Map a service model to the corresponding API model</summary>
        public DeviceModelSimulation(DeviceModel.StateSimulation state) : this()
        {
            if (state == null) return;

            this.Initial = state.InitialState;
            this.Interval = state.Interval.ToString("c");
            this.SimulationScripts = state.Scripts.Select(s => new DeviceModelSimulationScript(s)).ToList();
        }
    }
}