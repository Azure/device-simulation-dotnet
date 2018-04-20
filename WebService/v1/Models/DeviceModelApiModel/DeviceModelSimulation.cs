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
        public Dictionary<string, object> InitialState { get; set; }

        [JsonProperty(PropertyName = "Interval", NullValueHandling = NullValueHandling.Ignore)]
        public string Interval { get; set; }

        [JsonProperty(PropertyName = "Scripts")]
        public List<DeviceModelSimulationScript> Scripts { get; set; }

        public DeviceModelSimulation()
        {
            this.InitialState = new Dictionary<string, object>();
            this.Interval = null;
            this.Scripts = new List<DeviceModelSimulationScript>();
        }

        // Map service model to API model
        public static DeviceModelSimulation FromServiceModel(DeviceModel.StateSimulation value)
        {
            if (value == null) return null;

            return new DeviceModelSimulation
            {
                InitialState = value.InitialState,
                Interval = value.Interval.ToString("c"),
                Scripts = value.Scripts.Select(DeviceModelSimulationScript.FromServiceModel).Where(x => x != null).ToList()
            };
        }
    }
}
