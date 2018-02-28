// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.PATH + "/[controller]"), ExceptionsFilter]
    public sealed class StatusController : Controller
    {
        private const string SERVICE_IS_HEALTHY = "Alive and well";

        private const string JSON_TRUE = "true";
        private const string JSON_FALSE = "false";

        private const string SIMULATION_RUNNING_KEY = "SimulationRunning";
        private const string PREPROVISIONED_IOTHUB_KEY = "PreprovisionedIoTHub";
        private const string PREPROVISIONED_IOTHUB_INUSE_KEY = "PreprovisionedIoTHubInUse";
        private const string PREPROVISIONED_IOTHUB_METRICS_KEY = "PreprovisionedIoTHubMetricsUrl";
        private const string ACTIVE_DEVICE_COUNT_KEY = "ActiveDeviceCount";
        private const string TOTAL_MESSAGES_COUNT_KEY = "TotalMessagesCount";
        private const string FAILED_MESSAGES_COUNT_KEY = "FailedMessagesCount";
        private const string MESSAGE_PER_SECOND_KEY = "MessagesPerSecond";
        private const string FAILED_DEVICE_CONNECTIONS_COUNT_KEY = "FailedDeviceConnectionsCount";
        private const string FAILED_DEVICE_TWIN_UPDATES_COUNT_KEY = "FailedDeviceTwinUpdatesCount";

        private readonly IPreprovisionedIotHub preprovisionedIotHub;
        private readonly IStorageAdapterClient storage;
        private readonly ISimulations simulations;
        private readonly ILogger log;
        private readonly IServicesConfig servicesConfig;
        private readonly IDeploymentConfig deploymentConfig;
        private readonly IIotHubConnectionStringManager connectionStringManager;
        private readonly ISimulationRunner simulationRunner;
        private readonly IRateLimiting rateReporter;

        public StatusController(
            IPreprovisionedIotHub preprovisionedIotHub,
            IStorageAdapterClient storage,
            ISimulations simulations,
            ILogger logger,
            IServicesConfig servicesConfig,
            IDeploymentConfig deploymentConfig,
            IIotHubConnectionStringManager connectionStringManager,
            ISimulationRunner simulationRunner,
            IRateLimiting rateLimiting)
        {
            this.preprovisionedIotHub = preprovisionedIotHub;
            this.storage = storage;
            this.simulations = simulations;
            this.log = logger;
            this.servicesConfig = servicesConfig;
            this.deploymentConfig = deploymentConfig;
            this.connectionStringManager = connectionStringManager;
            this.simulationRunner = simulationRunner;
            this.rateReporter = rateLimiting;
        }

        [HttpGet]
        public async Task<StatusApiModel> Get()
        {
            var result = new StatusApiModel();
            var statusMsg = SERVICE_IS_HEALTHY;
            var errors = new List<string>();

            // Simulation status
            var simulationIsRunning = await this.IsSimulationRunning(errors);
            var isRunning = simulationIsRunning.HasValue && simulationIsRunning.Value;
            result.Properties.Add(SIMULATION_RUNNING_KEY,
                simulationIsRunning.HasValue
                    ? (isRunning ? JSON_TRUE : JSON_FALSE)
                    : "unknown");

            // Storage status
            var storageStatus = await this.CheckStorageStatus(errors);
            result.Dependencies.Add("Storage", storageStatus?.Item2);
            var statusIsOk = storageStatus.Item1;

            // Preprovisioned hub status
            var isHubPreprovisioned = this.IsHubConnectionStringConfigured();
            result.Properties.Add(PREPROVISIONED_IOTHUB_KEY,
                isHubPreprovisioned
                    ? JSON_TRUE
                    : JSON_FALSE);
            if (isHubPreprovisioned)
            {
                var preprovisioneHubStatus = await this.CheckAzureIoTHubStatus(errors);
                statusIsOk = statusIsOk && preprovisioneHubStatus.Item1;

                result.Dependencies.Add(PREPROVISIONED_IOTHUB_KEY, preprovisioneHubStatus?.Item2);
                if (isRunning)
                {
                    result.Properties.Add(PREPROVISIONED_IOTHUB_INUSE_KEY, this.IsPreprovisionedIoTHubInUse(isRunning));
                    var url = this.GetIoTHubMetricsUrl(isRunning);
                    if (!string.IsNullOrEmpty(url))
                    {
                        result.Properties.Add(PREPROVISIONED_IOTHUB_METRICS_KEY, this.GetIoTHubMetricsUrl(isRunning));
                    }
                }
            }

            // Active devices status
            string activeDeviceCount = this.GetActiveDevicesCount(isRunning).ToString();
            result.Properties.Add(ACTIVE_DEVICE_COUNT_KEY, activeDeviceCount);

            // Total telemetry messages count
            string totalMessagesCount = this.GetTotalMessagesCount(isRunning).ToString();
            result.Properties.Add(TOTAL_MESSAGES_COUNT_KEY, totalMessagesCount);

            // Failed telemetry messages count
            string failedMessagesCount = this.GetFailedMessagesCount(isRunning).ToString();
            result.Properties.Add(FAILED_MESSAGES_COUNT_KEY, failedMessagesCount);

            // Telemetry messages thoughput
            string messagesPerSecond = this.GetMessagesPerSecond(isRunning).ToString("F");
            result.Properties.Add(MESSAGE_PER_SECOND_KEY, messagesPerSecond);

            // Failed device connections count
            string failedDeviceConnectionsCount = this.GetFailedDeviceConnections(isRunning).ToString();
            result.Properties.Add(FAILED_DEVICE_CONNECTIONS_COUNT_KEY, failedDeviceConnectionsCount);

            // Failed device connections count
            string failedDeviceTwinUpdatesCount = this.GetFailedDeviceTwinUpdates(isRunning).ToString();
            result.Properties.Add(FAILED_DEVICE_TWIN_UPDATES_COUNT_KEY, failedDeviceTwinUpdatesCount);

            // Prepare status message and response
            if (!statusIsOk)
            {
                statusMsg = string.Join(";", errors);
            }

            result.SetStatus(statusIsOk, statusMsg);

            this.log.Info("Service status request", () => new
            {
                Healthy = statusIsOk,
                statusMsg,
                running = simulationIsRunning
            });

            return result;
        }

        // Check whether the simulation is running, and populate errors if any
        private async Task<bool?> IsSimulationRunning(List<string> errors)
        {
            bool? simulationRunning = null;
            try
            {
                var simulation = (await this.simulations.GetListAsync()).FirstOrDefault();
                simulationRunning = (simulation != null && simulation.ShouldBeRunning());
            }
            catch (Exception e)
            {
                errors.Add("Unable to fetch simulation status");
                this.log.Error("Unable to fetch simulation status", () => new { e });
            }

            return simulationRunning;
        }

        // Check the storage dependency status
        private async Task<Tuple<bool, string>> CheckStorageStatus(ICollection<string> errors)
        {
            Tuple<bool, string> result;
            try
            {
                result = await this.storage.PingAsync();
                if (!result.Item1)
                {
                    errors.Add("Unable to use Storage");
                }
            }
            catch (Exception e)
            {
                result = new Tuple<bool, string>(false, "Storage check failed");
                this.log.Error("Storage ping failed", () => new { e });
            }

            return result;
        }

        // Check IoT Hub dependency status
        private async Task<Tuple<bool, string>> CheckAzureIoTHubStatus(ICollection<string> errors)
        {
            Tuple<bool, string> result;
            try
            {
                if (this.IsHubConnectionStringConfigured())
                {
                    result = await this.preprovisionedIotHub.PingRegistryAsync();
                    if (!result.Item1)
                    {
                        errors.Add("Unable to use Azure IoT Hub service");
                    }
                }
                else
                {
                    result = new Tuple<bool, string>(true, "IoTHub connection string not configured");
                }
            }
            catch (Exception e)
            {
                result = new Tuple<bool, string>(false, "IoTHub check failed");
                this.log.Error("IoT Hub ping failed", () => new { e });
            }

            return result;
        }

        // Check whether the configuration contains a connection string
        private bool IsHubConnectionStringConfigured()
        {
            var cs = this.servicesConfig?.IoTHubConnString?.ToLowerInvariant().Trim();
            return (!string.IsNullOrEmpty(cs)
                    && cs.Contains("hostname=")
                    && cs.Contains("sharedaccesskeyname=")
                    && cs.Contains("sharedaccesskey="));
        }

        // Check whether the simulation is running with the conn string in the configuration
        private string IsPreprovisionedIoTHubInUse(bool isRunning)
        {
            if (!isRunning) return JSON_FALSE;

            var csInUse = this.connectionStringManager.GetIotHubConnectionString().ToLowerInvariant().Trim();
            var csInConf = this.servicesConfig?.IoTHubConnString?.ToLowerInvariant().Trim();

            return csInUse == csInConf ? JSON_TRUE : JSON_FALSE;
        }

        // If the simulation is running with the conn string in the config then return a URL to the metrics
        private string GetIoTHubMetricsUrl(bool isRunning)
        {
            if (!isRunning) return string.Empty;

            var csInUse = this.connectionStringManager.GetIotHubConnectionString().ToLowerInvariant().Trim();
            var csInConf = this.servicesConfig?.IoTHubConnString?.ToLowerInvariant().Trim();

            // Return the URL only when the simulation is running with the configured conn string
            if (csInUse != csInConf) return string.Empty;

            return $"https://portal.azure.com/{this.deploymentConfig.AzureSubscriptionDomain}" +
                   $"#resource/subscriptions/{this.deploymentConfig.AzureSubscriptionId}" +
                   $"/resourceGroups/{this.deploymentConfig.AzureResourceGroup}" +
                   $"/providers/Microsoft.Devices/IotHubs/{this.deploymentConfig.AzureIothubName}/Metrics";
        }

        private int GetActiveDevicesCount(bool isRunning)
        {
            if (!isRunning) return 0;

            return this.simulationRunner.GetActiveDevicesCount();
        }

        private int GetTotalMessagesCount(bool isRunning)
        {
            if (!isRunning) return 0;

            return this.simulationRunner.GetTotalMessagesCount();
        }

        private int GetFailedMessagesCount(bool isRunning)
        {
            if (!isRunning) return 0;

            return this.simulationRunner.GetFailedMessagesCount();
        }

        private double GetMessagesPerSecond(bool isRunning)
        {
            if (!isRunning) return 0;

            return this.rateReporter.GetThroughputForMessages();
        }

        private double GetFailedDeviceConnections(bool isRunning)
        {
            if (!isRunning) return 0;

            return this.simulationRunner.GetFailedDeviceConnections();
        }

        private double GetFailedDeviceTwinUpdates(bool isRunning)
        {
            if (!isRunning) return 0;

            return this.simulationRunner.GetFailedDeviceTwinUpdates();
        }
    }
}
