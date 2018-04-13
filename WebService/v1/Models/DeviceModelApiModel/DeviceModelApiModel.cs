// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

// TODO: tests
// TODO: handle errors
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel
{
    public class DeviceModelApiModel
    {
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Version")]
        public string Version { get; set; }

        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "Description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "Protocol")]
        public string Protocol { get; set; }

        [JsonProperty(PropertyName = "Simulation")]
        public DeviceModelSimulation Simulation { get; set; }

        [JsonProperty(PropertyName = "Properties")]
        public IDictionary<string, object> Properties { get; set; }

        [JsonProperty(PropertyName = "Telemetry")]
        public IList<DeviceModelTelemetry> Telemetry { get; set; }

        [JsonProperty(PropertyName = "CloudToDeviceMethods")]
        public IDictionary<string, DeviceModelSimulationScript> CloudToDeviceMethods { get; set; }

        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceModel;" + v1.Version.NUMBER },
            { "$uri", "/" + v1.Version.PATH + "/devicemodels/" + this.Id }
        };

        public DeviceModelApiModel()
        {
            this.Id = string.Empty;
            this.Version = string.Empty;
            this.Name = string.Empty;
            this.Description = string.Empty;
            this.Protocol = string.Empty;
            this.Simulation = new DeviceModelSimulation();
            this.Properties = new Dictionary<string, object>();
            this.Telemetry = new List<DeviceModelTelemetry>();
            this.CloudToDeviceMethods = new Dictionary<string, DeviceModelSimulationScript>();
        }

        // Map service model to API model
        public static DeviceModelApiModel FromServiceModel(DeviceModel value)
        {
            if (value == null) return null;

            var result = new DeviceModelApiModel
            {
                Id = value.Id,
                Version = value.Version,
                Name = value.Name,
                Description = value.Description,
                Protocol = value.Protocol.ToString(),
                Simulation = DeviceModelSimulation.FromServiceModel(value.Simulation)
            };

            foreach (var property in value.Properties)
            {
                result.Properties.Add(property.Key, property.Value);
            }

            foreach (var message in value.Telemetry)
            {
                result.Telemetry.Add(DeviceModelTelemetry.FromServiceModel(message));
            }

            foreach (var method in value.CloudToDeviceMethods)
            {
                result.CloudToDeviceMethods.Add(method.Key, DeviceModelSimulationScript.FromServiceModel(method.Value));
            }

            return result;
        }
    }
}
