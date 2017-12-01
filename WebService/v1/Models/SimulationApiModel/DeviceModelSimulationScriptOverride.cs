// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    // SEE: <DeviceModelApiModel.DeviceModelSimulationScript> for the original fields being overridden
    // Avoid subclassing <DeviceModelSimulationScript> to exclude unused fields and different default values
    public class DeviceModelSimulationScriptOverride
    {
        // Optional, used to change the script used
        [JsonProperty(PropertyName = "Type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        // Optional, used to change the script used
        [JsonProperty(PropertyName = "Path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }

        // Optional, used to provide input parameters to the script
        // TODO: validate input using a method provided by the function specified in this.Path
        //       https://github.com/Azure/device-simulation-dotnet/issues/139
        [JsonProperty(PropertyName = "Params", NullValueHandling = NullValueHandling.Ignore)]
        public object Params { get; set; }

        // Default constructor used by web service requests
        public DeviceModelSimulationScriptOverride()
        {
            this.Type = null;
            this.Path = null;
            this.Params = null;
        }

        // Map API model to service model
        public Simulation.DeviceModelSimulationScriptOverride ToServiceModel()
        {
            if (this.IsEmpty()) return null;

            return new Simulation.DeviceModelSimulationScriptOverride
            {
                Type = !string.IsNullOrEmpty(this.Type) ? this.Type : null,
                Path = !string.IsNullOrEmpty(this.Path) ? this.Path : null,
                Params = this.Params
            };
        }

        // Map service model to API model
        public static IList<DeviceModelSimulationScriptOverride> FromServiceModel(IList<Simulation.DeviceModelSimulationScriptOverride> value)
        {
            return value?.Select(FromServiceModel).Where(x => x != null && !x.IsEmpty()).ToList();
        }

        // Map service model to API model
        public static DeviceModelSimulationScriptOverride FromServiceModel(Simulation.DeviceModelSimulationScriptOverride value)
        {
            if (value == null) return null;

            return new DeviceModelSimulationScriptOverride
            {
                Type = !string.IsNullOrEmpty(value.Type) ? value.Type : null,
                Path = !string.IsNullOrEmpty(value.Path) ? value.Path : null,
                Params = value.Params
            };
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(this.Type)
                   && string.IsNullOrEmpty(this.Path)
                   && this.Params == null;
        }
    }
}