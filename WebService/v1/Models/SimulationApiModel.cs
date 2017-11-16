// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class SimulationApiModel
    {
        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";
        private readonly long version;
        private DateTimeOffset created;
        private DateTimeOffset modified;

        [JsonProperty(PropertyName = "ETag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Enabled")]
        public bool? Enabled { get; set; }

<<<<<<< HEAD
        [JsonProperty(PropertyName = "IoTHub")]
        public IotHubModelRef IotHub { get; set; }
=======
        [JsonProperty(PropertyName = "StartTime", NullValueHandling = NullValueHandling.Ignore)]
        public string StartTime { get; set; }

        [JsonProperty(PropertyName = "EndTime", NullValueHandling = NullValueHandling.Ignore)]
        public string EndTime { get; set; }
>>>>>>> 8dcebc065f5bd72a2a5e7f40dc6a052de94bbd8b

        [JsonProperty(PropertyName = "DeviceModels")]
        public List<DeviceModelRef> DeviceModels { get; set; }

        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "Simulation;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/simulations/" + this.Id },
            { "$version", this.version.ToString() },
            { "$created", this.created.ToString(DATE_FORMAT) },
            { "$modified", this.modified.ToString(DATE_FORMAT) }
        };

        public SimulationApiModel()
        {
            this.Id = string.Empty;

            // When unspecified, a simulation is enabled
            this.Enabled = true;

            this.StartTime = null;
            this.EndTime = null;
            this.DeviceModels = new List<DeviceModelRef>();
        }

        /// <summary>Map a service model to the corresponding API model</summary>
        public SimulationApiModel(Simulation simulation) : this()
        {
            this.ETag = simulation.ETag;
            this.Id = simulation.Id;
            this.Enabled = simulation.Enabled;

            // Ignore the date if the simulation doesn't have a start time
            if (simulation.StartTime.HasValue && !simulation.StartTime.Value.Equals(DateTimeOffset.MinValue))
            {
                this.StartTime = simulation.StartTime?.ToString(DATE_FORMAT);
            }

            // Ignore the date if the simulation doesn't have an end time
            if (simulation.EndTime.HasValue && !simulation.EndTime.Value.Equals(DateTimeOffset.MaxValue))
            {
                this.EndTime = simulation.EndTime?.ToString(DATE_FORMAT);
            }

            foreach (var x in simulation.DeviceModels)
            {
                var dt = new DeviceModelRef
                {
                    Id = x.Id,
                    Count = x.Count
                };
                this.DeviceModels.Add(dt);
            }

            this.version = simulation.Version;
            this.created = simulation.Created;
            this.modified = simulation.Modified;
        }

        public class DeviceModelRef
        {
            [JsonProperty(PropertyName = "Id")]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "Count")]
            public int Count { get; set; }
        }

        public class IotHubModelRef
        {
            [JsonProperty(PropertyName = "ConnectionString")]
            public string ConnectionString { get; set; }

        }

        /// <summary>Map an API model to the corresponding service model</summary>
        /// <param name="id">The simulation ID when using PUT/PATCH, empty otherwise</param>
        public Simulation ToServiceModel(string id = "")
        {
            this.Id = id;

            var now = DateTimeOffset.UtcNow;

            var result = new Simulation
            {
                ETag = this.ETag,
                Id = this.Id,
                StartTime = DateHelper.ParseDateExpression(this.StartTime, now),
                EndTime = DateHelper.ParseDateExpression(this.EndTime, now),

                // When unspecified, a simulation is enabled
                Enabled = this.Enabled ?? true
            };

            foreach (var x in this.DeviceModels)
            {
                var dt = new Simulation.DeviceModelRef
                {
                    Id = x.Id,
                    Count = x.Count
                };
                result.DeviceModels.Add(dt);
            }

            if (result.StartTime.HasValue && result.EndTime.HasValue
                && result.StartTime.Value.Ticks >= result.EndTime.Value.Ticks)
            {
                throw new InvalidSimulationSchedulingException("The end time must be after the start time");
            }

            return result;
        }

        /// <summary>
        /// Sets the connection string and removes the sensitive key data to be stored locally
        /// to the machine in a file .
        /// 
        /// TODO Encryption for key & storage in documentDb instead of file
        /// 
        /// </summary>
        /// <param name="connString"></param>
        private void SetConnectionString(string connString)
        {
            var key = this.GetKeyFromConnString(connString);

            // store in local file
            this.WriteKeyToFile(key);

            // redact key from connection string
            connString = connString.Replace(key, "");

            this.IotHub.ConnectionString = connString;
        }

        private string GetKeyFromConnString(string connString)
        {
            var match = Regex.Match(connString,
                @"^HostName=(?<hostName>.*);SharedAccessKeyName=(?<keyName>.*);SharedAccessKey=(?<key>.*);$");

            if (!match.Success)
            {
                var message = "Invalid connection string for IoTHub";
                throw new InvalidInputException(message);
            }

            return match.Groups["key"].Value;
        }

        private void WriteKeyToFile(string key)
        {
            string path = @"custom_iothub_key.txt";
            if (!File.Exists(path))
            {
                // Create a file to write to. 
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(key);
                }
            }
            else
            {
                File.WriteAllText(path, key);
            }
        }
    }
}
