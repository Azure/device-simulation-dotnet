// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    // SEE: <DeviceModelApiModel> for the original fields being overridden
    // Avoid subclassing <DeviceModelApiModel> to exclude unused fields and different default values
    public class DeviceModelApiModelOverride
    {
        // Optional field, used to customize the scripts which update the device state
        [JsonProperty(PropertyName = "Simulation", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceModelSimulationOverride Simulation { get; set; }

        // Optional field, the list can be empty
        // When non empty, the content is merged with (overwriting) the scripts in the device model definition
        // If this list is shorter than the original definition, elements in excess are removed
        // i.e. to keep all the original telemetry messages, there must be an entry for each of them
        [JsonProperty(PropertyName = "Telemetry", NullValueHandling = NullValueHandling.Ignore)]
        public IList<DeviceModelTelemetryOverride> Telemetry { get; set; }

        // Default constructor used by web service requests
        public DeviceModelApiModelOverride()
        {
            this.Simulation = null;
            this.Telemetry = null;
        }

        // Map API model to service model
        public Simulation.DeviceModelOverride ToServiceModel()
        {
            if (this.IsEmpty()) return null;

            var result = new Simulation.DeviceModelOverride
            {
                Simulation = this.Simulation?.ToServiceModel()
            };

            var items = this.Telemetry?.Where(x => !x.IsEmpty()).ToList();
            if (items?.Count > 0)
            {
                result.Telemetry = this.Telemetry.Where(
                    x => !x.IsEmpty()).Select(x => x.ToServiceModel()).ToList();
            }

            return result;
        }

        // Map service model to API model
        public static DeviceModelApiModelOverride FromServiceModel(Simulation.DeviceModelOverride value)
        {
            if (value == null) return null;

            return new DeviceModelApiModelOverride
            {
                Simulation = DeviceModelSimulationOverride.FromServiceModel(value.Simulation),
                Telemetry = DeviceModelTelemetryOverride.FromServiceModel(value.Telemetry)
            };
        }

        public bool IsEmpty()
        {
            return (this.Simulation == null || this.Simulation.IsEmpty())
                   && (this.Telemetry == null || this.Telemetry.Count == 0);
        }
    }
}