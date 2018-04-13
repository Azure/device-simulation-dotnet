// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    // SEE: <DeviceModelApiModel.DeviceModelTelemetry> for the original fields being overridden
    // Avoid subclassing <DeviceModelTelemetry> to exclude unused fields and different default values
    public class DeviceModelTelemetryOverride
    {
        // Optional field, used to customize the telemetry interval
        [JsonProperty(PropertyName = "Interval", NullValueHandling = NullValueHandling.Ignore)]
        public string Interval { get; set; }

        // Optional field, when null use the template set in the device model definition
        [JsonProperty(PropertyName = "MessageTemplate", NullValueHandling = NullValueHandling.Ignore)]
        public string MessageTemplate { get; set; }

        // Optional field, when null use the schema set in the device model definition
        [JsonProperty(PropertyName = "MessageSchema", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceModelTelemetryMessageSchemaOverride MessageSchema { get; set; }

        // Default constructor used by web service requests
        public DeviceModelTelemetryOverride()
        {
            this.Interval = null;
            this.MessageTemplate = null;
            this.MessageSchema = null;
        }

        // Map API model to service model
        public Simulation.DeviceModelTelemetryOverride ToServiceModel()
        {
            if (this.IsEmpty()) return null;

            var result = new Simulation.DeviceModelTelemetryOverride
            {
                MessageTemplate = !string.IsNullOrEmpty(this.MessageTemplate) ? this.MessageTemplate : null,
                MessageSchema = this.MessageSchema?.ToServiceModel()
            };

            if (!string.IsNullOrEmpty(this.Interval))
            {
                result.Interval = TimeSpan.Parse(this.Interval);
            }

            return result;
        }

        // Map service model to API model
        public static DeviceModelTelemetryOverride FromServiceModel(Simulation.DeviceModelTelemetryOverride value)
        {
            if (value == null) return null;

            return new DeviceModelTelemetryOverride
            {
                Interval = value.Interval?.ToString("c"),
                MessageTemplate = !string.IsNullOrEmpty(value.MessageTemplate) ? value.MessageTemplate : null,
                MessageSchema = DeviceModelTelemetryMessageSchemaOverride.FromServiceModel(value.MessageSchema)
            };
        }

        // Map service model to API model
        public static IList<DeviceModelTelemetryOverride> FromServiceModel(IEnumerable<Simulation.DeviceModelTelemetryOverride> value)
        {
            return value?.Select(FromServiceModel).ToList();
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(this.Interval)
                   && string.IsNullOrEmpty(this.MessageTemplate)
                   && (this.MessageSchema == null || this.MessageSchema.IsEmpty());
        }
    }
}