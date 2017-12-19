// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class SimulationDeviceModelRef
    {
        // Mandatory field, must be provided
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        // Mandatory field, 0 is not valid
        [JsonProperty(PropertyName = "Count")]
        public int Count { get; set; }

        // Optional field, nothing to override when null
        [JsonProperty(PropertyName = "Override", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceModelApiModelOverride Override { get; set; }

        // Default constructor used by web service requests
        public SimulationDeviceModelRef()
        {
            this.Id = string.Empty;
            this.Count = 1;
            this.Override = null;
        }

        // Map API model to service model
        public Simulation.DeviceModelRef ToServiceModel()
        {
            // Map "Custom" and "CUSTOM" to "custom", i.e. ignore case
            var id = this.Id;
            if (string.Compare(id, DeviceModels.CUSTOM_DEVICE_MODEL_ID, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                id = DeviceModels.CUSTOM_DEVICE_MODEL_ID;
            }

            return new Simulation.DeviceModelRef
            {
                Id = id,
                Count = this.Count,
                Override = this.Override?.ToServiceModel()
            };
        }

        // Map service model to API model
        public static IList<SimulationDeviceModelRef> FromServiceModel(IEnumerable<Simulation.DeviceModelRef> value)
        {
            return value?.Select(FromServiceModel).ToList();
        }

        // Map service model to API model
        public static SimulationDeviceModelRef FromServiceModel(Simulation.DeviceModelRef value)
        {
            if (value == null) return null;

            return new SimulationDeviceModelRef
            {
                Id = value.Id,
                Count = value.Count,
                Override = DeviceModelApiModelOverride.FromServiceModel(value.Override)
            };
        }
    }
}