// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Newtonsoft.Json;

// TODO: tests
// TODO: handle errors
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel
{
    public class DeviceModelApiModel
    {
        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";

        private DateTimeOffset created;
        private DateTimeOffset modified;

        [JsonProperty(PropertyName = "ETag")]
        public string ETag { get; set; }

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

        [JsonProperty(PropertyName = "Type")]
        public string Type { get; set; }

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
            { "$uri", "/" + v1.Version.PATH + "/devicemodels/" + this.Id },
            { "$created", this.created.ToString(DATE_FORMAT) },
            { "$modified", this.modified.ToString(DATE_FORMAT) }
        };

        public DeviceModelApiModel()
        {
            this.ETag = string.Empty;
            this.Id = string.Empty;
            this.Version = string.Empty;
            this.Name = string.Empty;
            this.Description = string.Empty;
            this.Protocol = string.Empty;
            this.Type = string.Empty;
            this.Simulation = new DeviceModelSimulation();
            this.Properties = new Dictionary<string, object>();
            this.Telemetry = new List<DeviceModelTelemetry>();
            this.CloudToDeviceMethods = new Dictionary<string, DeviceModelSimulationScript>();
        }

        // Map API model to service model
        public DeviceModel ToServiceModel(string id = "")
        {
            this.Id = id;
            Enum.TryParse(this.Type, true, out DeviceModel.DeviceModelType type);

            var result = new DeviceModel
            {
                ETag = this.ETag,
                Id = this.Id,
                Version = this.Version,
                Name = this.Name,
                Description = this.Description,
                Type = type,
                Protocol = (IoTHubProtocol)Enum.Parse(typeof(IoTHubProtocol), this.Protocol, true),
                Simulation = DeviceModelSimulation.ToServiceModel(this.Simulation),
                Properties = new Dictionary<string, object>(this.Properties),
                Telemetry = this.Telemetry.Select(DeviceModelTelemetry.ToServiceModel).ToList(),
                CloudToDeviceMethods = null
            };

            // Map the list of CloudToDeviceMethods
            if (this.CloudToDeviceMethods != null && this.CloudToDeviceMethods.Count > 0)
            {
                result.CloudToDeviceMethods = new Dictionary<string, Script>();
                foreach (KeyValuePair<string, DeviceModelSimulationScript> method in this.CloudToDeviceMethods)
                {
                    var fieldValue = method.Value.ToServiceModel();
                    result.CloudToDeviceMethods.Add(method.Key, fieldValue);
                }
            }

            return result;
        }

        // Map service model to API model
        public static DeviceModelApiModel FromServiceModel(DeviceModel value)
        {
            if (value == null) return null;

            var result = new DeviceModelApiModel
            {
                ETag = value.ETag,
                Id = value.Id,
                Version = value.Version,
                Name = value.Name,
                Description = value.Description,
                Type = value.Type.ToString(),
                created = value.Created,
                modified = value.Modified,
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

            if (value.CloudToDeviceMethods?.Count > 0)
            {
                foreach (var method in value.CloudToDeviceMethods)
                {
                    result.CloudToDeviceMethods.Add(method.Key, DeviceModelSimulationScript.FromServiceModel(method.Value));
                }
            }

            return result;
        }

        public void ValidateInputRequest(ILogger log)
        {
            const string NO_PROTOCOL = "The device model doesn't contain a protocol";
            const string ZERO_TELEMETRY = "The device model has zero telemetry";
            const string INVALID_TYPE = "The device model has an invalid type";

            // We accept empty string, null and 'Custom' values
            if (!string.IsNullOrEmpty(this.Type)
                && (!Enum.TryParse(this.Type, true, out DeviceModel.DeviceModelType type)
                    || type != DeviceModel.DeviceModelType.Custom))
            {
                log.Error(INVALID_TYPE, () => new { deviceModel = this });
                throw new BadRequestException(INVALID_TYPE);
            }

            // A device model must contain a protocol
            if (this.Protocol == String.Empty)
            {
                log.Error(NO_PROTOCOL, () => new { deviceModel = this });
                throw new BadRequestException(NO_PROTOCOL);
            }

            this.ValidateProtocol(this.Protocol);

            // A device model must contain at least one telemetry
            if (this.Telemetry.Count < 1)
            {
                log.Error(ZERO_TELEMETRY, () => new { deviceModel = this });
                throw new BadRequestException(ZERO_TELEMETRY);
            }

            // Validate telemetry
            foreach (var telemetry in this.Telemetry)
            {
                telemetry.ValidateInputRequest(log);
            }

            this.Simulation.ValidateInputRequest(log);
        }

        private void ValidateProtocol(string protocol)
        {
            if (!Enum.TryParse(protocol, true, out IoTHubProtocol _))
            {
                throw new BadRequestException(protocol + " is not a member of the IoTHubProtocol enumeration.");
            }
        }
    }
}
