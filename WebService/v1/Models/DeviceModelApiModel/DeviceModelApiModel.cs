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
            this.Simulation = new DeviceModelSimulation();
            this.Telemetry = new List<DeviceModelTelemetry>();
            this.Properties = new Dictionary<string, object>();
            this.CloudToDeviceMethods = new Dictionary<string, DeviceModelSimulationScript>();
        }

        /// <summary>Map a service model to the corresponding API model</summary>
        public DeviceModelApiModel(DeviceModel model) : this()
        {
            if (model == null) return;

            this.Id = model.Id;
            this.Version = model.Version;
            this.Name = model.Name;
            this.Description = model.Description;
            this.Protocol = model.Protocol.ToString();
            this.Simulation = new DeviceModelSimulation(model.Simulation);

            foreach (var property in model.Properties)
            {
                this.Properties.Add(property.Key, property.Value);
            }

            foreach (var message in model.Telemetry)
            {
                this.Telemetry.Add(new DeviceModelTelemetry(message));
            }

            foreach (var method in model.CloudToDeviceMethods)
            {
                this.CloudToDeviceMethods.Add(method.Key, new DeviceModelSimulationScript(method.Value));
            }
        }
    }
}
