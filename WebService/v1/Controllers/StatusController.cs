// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.PATH + "/[controller]"), ExceptionsFilter]
    public sealed class StatusController : Controller
    {
        private const string JSON_TRUE = "true";
        private const string JSON_FALSE = "false";

        private readonly IDevices devices;
        private readonly IStorageAdapterClient storage;
        private readonly ISimulations simulations;
        private readonly ILogger log;
        private readonly IServicesConfig servicesConfig;
        private readonly IDeploymentConfig deploymentConfig;

        public StatusController(
            IDevices devices,
            IStorageAdapterClient storage,
            ISimulations simulations,
            ILogger logger,
            IServicesConfig servicesConfig,
            IDeploymentConfig deploymentConfig)
        {
            this.devices = devices;
            this.storage = storage;
            this.simulations = simulations;
            this.log = logger;
            this.servicesConfig = servicesConfig;
            this.deploymentConfig = deploymentConfig;
        }

        [HttpGet]
        public async Task<StatusApiModel> Get()
        {
            var statusMsg = "Alive and well";
            var errors = new List<string>();

            var iotHubStatus = await this.CheckAzureIoTHubStatus(errors);
            var storageStatus = await this.CheckStorageStatus(errors);
            var simulationIsRunning = await this.IsSimulationRunning(errors);

            // Prepare status message
            var statusIsOk = iotHubStatus.Item1 && storageStatus.Item1;
            if (!statusIsOk)
            {
                statusMsg = string.Join(";", errors);
            }

            // Prepare response
            var result = new StatusApiModel(statusIsOk, statusMsg);
            var isHubConfigured = this.IsHubConnectionStringConfigured();

            result.Properties.Add("SimulationRunning",
                simulationIsRunning.HasValue
                    ? (simulationIsRunning.Value ? JSON_TRUE : JSON_FALSE)
                    : "unknown");
            result.Properties.Add("IoTHubConnectionStringConfigured",
                isHubConfigured
                    ? JSON_TRUE
                    : JSON_FALSE);

            if (isHubConfigured)
            {
                result.Properties.Add("IoTHubMetricsUrl", this.GetIoTHubMetricsUrl());
            }

            result.Dependencies.Add("IoTHub", iotHubStatus?.Item2);
            result.Dependencies.Add("Storage", storageStatus?.Item2);

            this.log.Info("Service status request", () => new { Healthy = statusIsOk, statusMsg, running = simulationIsRunning });

            return result;
        }

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

        private async Task<Tuple<bool, string>> CheckAzureIoTHubStatus(ICollection<string> errors)
        {
            Tuple<bool, string> result;
            try
            {
                if (this.IsHubConnectionStringConfigured())
                {
                    result = await this.devices.PingRegistryAsync();
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
                this.log.Error("Hub ping failed", () => new { e });
            }

            return result;
        }

        private bool IsHubConnectionStringConfigured()
        {
            var cs = this.servicesConfig?.IoTHubConnString?.ToLowerInvariant().Trim();
            return (!string.IsNullOrEmpty(cs)
                    && cs.Contains("hostname=")
                    && cs.Contains("sharedaccesskeyname=")
                    && cs.Contains("sharedaccesskey="));
        }

        private string GetIoTHubMetricsUrl()
        {
            return $"https://portal.azure.com/{this.deploymentConfig.AzureSubscriptionDomain}" +
                   $"#resource/subscriptions/{this.deploymentConfig.AzureSubscriptionId}" +
                   $"/resourceGroups/{this.deploymentConfig.AzureResourceGroup}" +
                   $"/providers/Microsoft.Devices/IotHubs/{this.deploymentConfig.AzureIothubName}/Metrics";
        }
    }
}
