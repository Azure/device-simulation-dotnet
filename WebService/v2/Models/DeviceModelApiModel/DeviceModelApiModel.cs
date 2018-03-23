// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Models.Helpers;
using Newtonsoft.Json;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Exceptions;

// TODO: tests
// TODO: handle errors
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Models.DeviceModelApiModel
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

        //[JsonProperty(PropertyName = "Created", NullValueHandling = NullValueHandling.Ignore)]
        //public string Created { get; set; }

        //[JsonProperty(PropertyName = "Modified", NullValueHandling = NullValueHandling.Ignore)]
        //public string Modified { get; set; }

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

            var now = DateTimeOffset.UtcNow;

            var result = new DeviceModel
            {
                ETag = this.ETag,
                Id = this.Id,
                Version = this.Version,
                Name = this.Name,
                Description = this.Description,
                Type = this.Type,
                Protocol = (IoTHubProtocol)Enum.Parse(typeof(IoTHubProtocol), this.Protocol, true),
                Simulation = DeviceModelSimulation.ToServiceModel(this.Simulation),
                Properties = new Dictionary<string, object>(this.Properties),
                Telemetry = this.Telemetry.Select(x => DeviceModelTelemetry.ToServiceModel(x)).ToList(),
                CloudToDeviceMethods = null
            };

            // TODO check if object works above
            // Map the list of Properties
            //if (this.Properties != null && this.Properties.Count > 0)
            //{
            //    result.Properties = new Dictionary<string, object>();
            //    foreach (KeyValuePair<string, object> prop in this.Properties)
            //    {
            //        var fieldValue = DeviceModelSimulationScript.ToServiceModel(prop.Value);
            //        result.CloudToDeviceMethods.Add(prop.Key, fieldValue);
            //    }
            //}

            // Map the list of CloudToDeviceMethods
            if (this.CloudToDeviceMethods != null && this.CloudToDeviceMethods.Count > 0)
            {
                result.CloudToDeviceMethods = new Dictionary<string, Script>();
                foreach (KeyValuePair<string, DeviceModelSimulationScript> method in this.CloudToDeviceMethods)
                {
                    var fieldValue = DeviceModelSimulationScript.ToServiceModel(method.Value);
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
                created = value.Created,
                modified =value.Modified,
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

        public void ValidateInputRequest(ILogger log)
        {
            const string NO_ETAG = "The custom device model doesn't contain a ETag";
            const string NO_PROTOCOL = "The device model doesn't contain a name";
            const string ZERO_TELEMETRY = "The simulation has zero telemetry";
            const string END_TIME_BEFORE_START_TIME = "The simulation End Time must be after the Start Time";
            const string INVALID_DATE = "Invalid date format";
            const string CANNOT_RUN_IN_THE_PAST = "The simulation end date is in the past";

            // A custom device model must contain a ETag
            if (this.Type == "CustomModel" && this.ETag == String.Empty)
            {
                log.Error(NO_ETAG, () => new { deviceModel = this });
                throw new BadRequestException(NO_ETAG);
            }

            // A device model must contain a protocol
            if (this.Protocol == String.Empty)
            {
                log.Error(NO_PROTOCOL, () => new { deviceModel = this });
                throw new BadRequestException(NO_PROTOCOL);
            }

            // A device model must contain at least one telemetry
            if (this.Telemetry.Count < 1)
            {
                log.Error(ZERO_TELEMETRY, () => new { deviceModel = this });
                throw new BadRequestException(ZERO_TELEMETRY);
            }

            try
            {
                foreach(var telemetry in this.Telemetry)
                {
                    telemetry.ValidateInputRequest(log);
                }

                // The start time must be before the end time
                if (startTime.HasValue && endTime.HasValue && startTime.Value.Ticks >= endTime.Value.Ticks)
                {
                    log.Error(END_TIME_BEFORE_START_TIME, () => new { simulation = this });
                    throw new BadRequestException(END_TIME_BEFORE_START_TIME);
                }

                // The end time cannot be in the past
                if (endTime.HasValue && endTime.Value.Ticks <= now.Ticks)
                {
                    log.Error(CANNOT_RUN_IN_THE_PAST, () => new { simulation = this });
                    throw new BadRequestException(CANNOT_RUN_IN_THE_PAST);
                }
            }
            catch (InvalidDateFormatException e)
            {
                log.Error(INVALID_DATE, () => new { simulation = this });
                throw new BadRequestException(INVALID_DATE, e);
            }
        }
    }
}
