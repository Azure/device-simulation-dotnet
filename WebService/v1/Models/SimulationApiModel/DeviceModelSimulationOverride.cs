// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    // SEE: <DeviceModelApiModel.DeviceModelSimulation> for the original fields being overridden
    public class DeviceModelSimulationOverride
    {
        // Optional, used to customize the device state update interval
        [JsonProperty(PropertyName = "Interval", NullValueHandling = NullValueHandling.Ignore)]
        public string Interval { get; set; }

        // Optional field, the list can be empty
        // When non empty, the content is merged with (overwriting) the scripts in the device model definition
        // If this list is shorter than the original definition, elements in excess are removed
        // i.e. to keep all the original scripts, there must be an entry for each of them
        [JsonProperty(PropertyName = "Scripts", NullValueHandling = NullValueHandling.Ignore)]
        public IList<DeviceModelSimulationScriptOverride> SimulationScripts { get; set; }

        // Default constructor used by web service requests
        public DeviceModelSimulationOverride()
        {
            this.Interval = null;
            this.SimulationScripts = null;
        }

        // Map API model to service model
        public Simulation.DeviceModelSimulationOverride ToServiceModel()
        {
            if (this.IsEmpty()) return null;

            var result = new Simulation.DeviceModelSimulationOverride();

            var scripts = this.SimulationScripts?.Where(x => x != null && !x.IsEmpty()).ToList();
            if (scripts?.Count > 0)
            {
                result.SimulationScripts = this.SimulationScripts.Select(x => x.ToServiceModel()).ToList();
            }

            if (!string.IsNullOrEmpty(this.Interval))
            {
                result.Interval = TimeSpan.Parse(this.Interval);
            }

            return result;
        }

        // Map service model to API model
        public static DeviceModelSimulationOverride FromServiceModel(Simulation.DeviceModelSimulationOverride value)
        {
            if (value == null) return null;

            return new DeviceModelSimulationOverride
            {
                Interval = value.Interval?.ToString("c"),
                SimulationScripts = DeviceModelSimulationScriptOverride.FromServiceModel(value.SimulationScripts)
            };
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(this.Interval)
                   && (this.SimulationScripts == null || this.SimulationScripts.Count == 0);
        }
    }
}