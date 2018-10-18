// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    // SEE: <DeviceModelApiModel.DeviceModelScript> for the original fields being overridden
    // Avoid subclassing <DeviceModelScript> to exclude unused fields and different default values
    public class DeviceModeScriptOverride
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
        public DeviceModeScriptOverride()
        {
            this.Type = null;
            this.Path = null;
            this.Params = null;
        }

        // Map API model to service model
        public Simulation.DeviceModelScriptOverride ToServiceModel()
        {
            if (this.IsEmpty()) return null;

            return new Simulation.DeviceModelScriptOverride
            {
                Type = !string.IsNullOrEmpty(this.Type) ? this.Type : null,
                Path = !string.IsNullOrEmpty(this.Path) ? this.Path : null,
                Params = this.Params
            };
        }

        // Map service model to API model
        public static IList<DeviceModeScriptOverride> FromServiceModel(IList<Simulation.DeviceModelScriptOverride> value)
        {
            return value?.Select(FromServiceModel).Where(x => x != null && !x.IsEmpty()).ToList();
        }

        // Map service model to API model
        public static DeviceModeScriptOverride FromServiceModel(Simulation.DeviceModelScriptOverride value)
        {
            if (value == null) return null;

            return new DeviceModeScriptOverride
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