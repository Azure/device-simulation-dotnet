// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class SimulationApiModel
    {
        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";

        private DateTimeOffset created;
        private DateTimeOffset modified;

        [JsonProperty(PropertyName = "ETag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "Description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "Enabled")]
        public bool? Enabled { get; set; }

        // Note: read-only property, used only to report the simulation status
        [JsonProperty(PropertyName = "Running")]
        public bool? Running { get; set; }

        [JsonProperty(PropertyName = "IoTHubs")]
        public IList<SimulationIotHub> IotHubs { get; set; }

        [JsonProperty(PropertyName = "StartTime")]
        public string StartTime { get; set; }

        [JsonProperty(PropertyName = "EndTime")]
        public string EndTime { get; set; }

        // Note: read-only property, used only to report the simulation status
        [JsonProperty(PropertyName = "StoppedTime")]
        public string StoppedTime { get; set; }

        [JsonProperty(PropertyName = "DeviceModels")]
        public IList<SimulationDeviceModelRef> DeviceModels { get; set; }

        // Note: read-only property, used only to report the simulation status
        [JsonProperty(PropertyName = "Statistics")]
        public SimulationStatistics Statistics { get; set; }

        // Note: read-only metadata
        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "Simulation;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/simulations/" + this.Id },
            { "$created", this.created.ToString(DATE_FORMAT) },
            { "$modified", this.modified.ToString(DATE_FORMAT) }
        };

        // Default constructor used by web service requests
        public SimulationApiModel()
        {
            this.Id = string.Empty;
            this.Name = string.Empty;
            this.Description = string.Empty;

            // When unspecified, a simulation is enabled
            this.Enabled = true;
            this.Running = false;
            this.IotHubs = new List<SimulationIotHub>();
            this.StartTime = null;
            this.EndTime = null;
            this.StoppedTime = null;
            this.DeviceModels = new List<SimulationDeviceModelRef>();
            this.Statistics = new SimulationStatistics();
        }

        // Map API model to service model, keeping the original fields when needed
        public Simulation ToServiceModel(Simulation existingSimulation, string id = "")
        {
            var now = DateTimeOffset.UtcNow;

            // ID can be empty, e.g. with POST requests
            this.Id = id;

            // Use the existing simulation fields if available, so that read-only values are not lost
            // e.g. the state of partitioning, device creation, etc.
            var result = new Simulation();
            if (existingSimulation != null)
            {
                result = existingSimulation;
            }

            result.ETag = this.ETag;
            result.Id = this.Id;
            result.Name = this.Name;
            result.Description = this.Description;
            result.StartTime = DateHelper.ParseDateExpression(this.StartTime, now);
            result.EndTime = DateHelper.ParseDateExpression(this.EndTime, now);
            result.DeviceModels = this.DeviceModels?.Select(x => x.ToServiceModel()).ToList();

            // Overwrite the value only if the request included the field, i.e. don't
            // enable/disable the simulation if the user didn't explicitly ask to.
            if (this.Enabled.HasValue)
            {
                result.Enabled = this.Enabled.Value;
            }

            foreach (var hub in this.IotHubs)
            {
                var connString = SimulationIotHub.ToServiceModel(hub);

                if (!result.IotHubConnectionStrings.Contains(connString))
                {
                    result.IotHubConnectionStrings.Add(connString);
                }
            }

            return result;
        }

        // Map service model to API model
        public static async Task<SimulationApiModel> FromServiceModelAsync(
            Simulation value,
            IServicesConfig servicesConfig,
            IDeploymentConfig deploymentConfig,
            IIotHubConnectionStringManager connectionStringManager,
            ISimulationRunner simulationRunner,
            IRateLimiting rateReporter)
        {
            if (value == null) return null;

            var result = new SimulationApiModel
            {
                ETag = value.ETag,
                Id = value.Id,
                Name = value.Name,
                Description = value.Description,
                Enabled = value.Enabled,
                Running = value.ShouldBeRunning,
                StartTime = value.StartTime.ToString(),
                EndTime = value.EndTime.ToString(),
                StoppedTime = value.StoppedTime.ToString(),
                IotHubs = new List<SimulationIotHub>()
            };

            foreach (var iotHubConnectionString in value.IotHubConnectionStrings)
            {
                var iotHub = new SimulationIotHub { ConnectionString = iotHubConnectionString };
                result.IotHubs.Add(iotHub);
            }

            // Ignore the date if the simulation doesn't have a start time
            if (value.StartTime.HasValue && !value.StartTime.Value.Equals(DateTimeOffset.MinValue))
            {
                result.StartTime = value.StartTime?.ToString(DATE_FORMAT);
            }

            // Ignore the date if the simulation doesn't have an end time
            if (value.EndTime.HasValue && !value.EndTime.Value.Equals(DateTimeOffset.MaxValue))
            {
                result.EndTime = value.EndTime?.ToString(DATE_FORMAT);
            }

            // Ignore the date if the simulation doesn't have an end time
            if (value.StoppedTime.HasValue && !value.StoppedTime.Value.Equals(DateTimeOffset.MaxValue))
            {
                result.StoppedTime = value.StoppedTime?.ToString(DATE_FORMAT);
            }

            result.DeviceModels = SimulationDeviceModelRef.FromServiceModel(value.DeviceModels);
            result.Statistics = SimulationStatistics.FromServiceModel(value.Statistics);
            result.created = value.Created;
            result.modified = value.Modified;

            await result.AppendHubPropertiesAndStatisticsAsync(servicesConfig, deploymentConfig, connectionStringManager, simulationRunner, rateReporter);

            return result;
        }

        public async Task ValidateInputRequestAsync(ILogger log, IIotHubConnectionStringManager connectionStringManager)
        {
            const string NO_DEVICE_MODEL = "The simulation doesn't contain any device model";
            const string ZERO_DEVICES = "The simulation has zero devices";
            const string END_TIME_BEFORE_START_TIME = "The simulation End Time must be after the Start Time";
            const string INVALID_DATE = "Invalid date format";
            const string CANNOT_RUN_IN_THE_PAST = "The simulation end date is in the past";
            const string NO_IOTHUB_CONNSTRING = "The simulation doesn't contain any IoTHub connection string";

            // A simulation must contain at least one device model
            if (this.DeviceModels.Count < 1)
            {
                log.Error(NO_DEVICE_MODEL, () => new { simulation = this });
                throw new BadRequestException(NO_DEVICE_MODEL);
            }

            // A simulation must use at least one device
            if (this.DeviceModels.Sum(x => x.Count) < 1)
            {
                log.Error(ZERO_DEVICES, () => new { simulation = this });
                throw new BadRequestException(ZERO_DEVICES);
            }

            try
            {
                var now = DateTimeOffset.UtcNow;
                var startTime = DateHelper.ParseDateExpression(this.StartTime, now);
                var endTime = DateHelper.ParseDateExpression(this.EndTime, now);
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

            // A simulation contains at least one iothub connect string
            if (this.IotHubs.Count == 0)
            {
                throw new BadRequestException(NO_IOTHUB_CONNSTRING);
            }

            foreach (var iotHub in this.IotHubs)
            {
                await connectionStringManager.ValidateConnectionStringAsync(iotHub.ConnectionString);
            }
        }

        // Append additional Hub properties and Statistics 
        private async Task AppendHubPropertiesAndStatisticsAsync(
            IServicesConfig servicesConfig,
            IDeploymentConfig deploymentConfig,
            IIotHubConnectionStringManager connectionStringManager,
            ISimulationRunner simulationRunner,
            IRateLimiting rateReporter)
        {
            var isRunning = this.Running == true;

            foreach (var iotHub in this.IotHubs)
            {
                // Preprovisioned IoT hub status
                var isHubPreprovisioned = this.IsHubConnectionStringConfigured(servicesConfig);

                if (isHubPreprovisioned && isRunning)
                {
                    iotHub.PreprovisionedIoTHubInUse = await this.IsPreprovisionedIoTHubInUseAsync(servicesConfig, connectionStringManager);
                    iotHub.PreprovisionedIoTHubMetricsUrl = await this.GetIoTHubMetricsUrlAsync(servicesConfig, deploymentConfig, connectionStringManager);
                }
            }

            if (isRunning)
            {
                // Average messages per second frequency in the last minutes
                this.Statistics.AverageMessagesPerSecond = rateReporter.GetThroughputForMessages();

                // Total messages count
                this.Statistics.TotalMessagesSent = simulationRunner.TotalMessagesCount;

                // Active devices count
                this.Statistics.ActiveDevicesCount = simulationRunner.ActiveDevicesCount;

                // Failed telemetry messages count
                this.Statistics.FailedMessagesCount = simulationRunner.FailedMessagesCount;

                // Failed device connections count
                this.Statistics.FailedDeviceConnectionsCount = simulationRunner.FailedDeviceConnectionsCount;

                // Failed device connections count
                this.Statistics.FailedDeviceTwinUpdatesCount = simulationRunner.FailedDeviceTwinUpdatesCount;

                // Simulation errors count
                this.Statistics.SimulationErrorsCount = simulationRunner.SimulationErrorsCount;
            }
        }

        // Check whether the configuration contains a connection string
        private bool IsHubConnectionStringConfigured(IServicesConfig servicesConfig)
        {
            var cs = servicesConfig?.IoTHubConnString?.ToLowerInvariant().Trim();
            return (!string.IsNullOrEmpty(cs)
                    && cs.Contains("hostname=")
                    && cs.Contains("sharedaccesskeyname=")
                    && cs.Contains("sharedaccesskey="));
        }

        // Check whether the simulation is running with the conn string in the configuration
        private async Task<bool> IsPreprovisionedIoTHubInUseAsync(IServicesConfig servicesConfig, IIotHubConnectionStringManager connectionStringManager)
        {
            var csInUse = (await connectionStringManager.GetConnectionStringAsync()).ToLowerInvariant().Trim();
            var csInConf = servicesConfig?.IoTHubConnString?.ToLowerInvariant().Trim();

            return csInUse == csInConf;
        }

        // If the simulation is running with the conn string in the config then return a URL to the metrics
        private async Task<string> GetIoTHubMetricsUrlAsync(IServicesConfig servicesConfig, IDeploymentConfig deploymentConfig, IIotHubConnectionStringManager connectionStringManager)
        {
            var csInUse = (await connectionStringManager.GetConnectionStringAsync()).ToLowerInvariant().Trim();
            var csInConf = servicesConfig?.IoTHubConnString?.ToLowerInvariant().Trim();

            // Return the URL only when the simulation is running with the configured conn string
            if (csInUse != csInConf) return string.Empty;

            return $"https://portal.azure.com/{deploymentConfig.AzureSubscriptionDomain}" +
                   $"#resource/subscriptions/{deploymentConfig.AzureSubscriptionId}" +
                   $"/resourceGroups/{deploymentConfig.AzureResourceGroup}" +
                   $"/providers/Microsoft.Devices/IotHubs/{deploymentConfig.AzureIothubName}/Metrics";
        }
    }
}
