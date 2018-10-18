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
        private const string SERVICE_IS_HEALTHY = "Alive and well";

        private const string JSON_TRUE = "true";
        private const string JSON_FALSE = "false";

        private const string SIMULATION_RUNNING_KEY = "SimulationRunning";
        private const string PREPROVISIONED_IOTHUB_KEY = "PreprovisionedIoTHub";

        private readonly IPreprovisionedIotHub preprovisionedIotHub;
        private readonly IStorageAdapterClient storage;
        private readonly ISimulations simulations;
        private readonly ILogger log;
        private readonly IServicesConfig servicesConfig;

        public StatusController(
            IPreprovisionedIotHub preprovisionedIotHub,
            IStorageAdapterClient storage,
            ISimulations simulations,
            ILogger logger,
            IServicesConfig servicesConfig)
        {
            this.preprovisionedIotHub = preprovisionedIotHub;
            this.storage = storage;
            this.simulations = simulations;
            this.log = logger;
            this.servicesConfig = servicesConfig;
        }

        // TODO: reduce method complexity, refactor out some logic
        [HttpGet]
        public async Task<StatusApiModel> GetAsync()
        {
            var result = new StatusApiModel();
            var statusMsg = SERVICE_IS_HEALTHY;
            var errors = new List<string>();

            // Simulation status
            var simulationIsRunning = await this.CheckIsSimulationRunningAsync(errors);
            var isRunning = simulationIsRunning.HasValue && simulationIsRunning.Value;
            result.Properties.Add(SIMULATION_RUNNING_KEY,
                simulationIsRunning.HasValue
                    ? (isRunning ? JSON_TRUE : JSON_FALSE)
                    : "unknown");

            // Storage status
            var storageStatus = await this.CheckStorageStatusAsync(errors);
            result.Dependencies.Add("Storage", storageStatus?.Item2);
            var statusIsOk = storageStatus.Item1;

            // Preprovisioned IoT hub status
            var isHubPreprovisioned = this.IsHubConnectionStringConfigured();
            result.Properties.Add(PREPROVISIONED_IOTHUB_KEY,
                isHubPreprovisioned
                    ? JSON_TRUE
                    : JSON_FALSE);
            if (isHubPreprovisioned)
            {
                var preprovisionedHubStatus = await this.CheckAzureIoTHubStatusAsync(errors);
                statusIsOk = statusIsOk && preprovisionedHubStatus.Item1;

                result.Dependencies.Add(PREPROVISIONED_IOTHUB_KEY, preprovisionedHubStatus?.Item2);
            }

            // Prepare status message and response
            if (!statusIsOk)
            {
                statusMsg = string.Join(";", errors);
            }

            result.SetStatus(statusIsOk, statusMsg);

            this.log.Info("Service status request", () => new
            {
                Healthy = statusIsOk,
                statusMsg
            });

            return result;
        }

        // Check whether the simulation is running, and populate errors if any
        private async Task<bool?> CheckIsSimulationRunningAsync(List<string> errors)
        {
            bool? simulationRunning = null;
            try
            {
                var simulationList = await this.simulations.GetListAsync();
                var runningSimulation = simulationList.FirstOrDefault(s => s.ShouldBeRunning);
                simulationRunning = (runningSimulation != null);
            }
            catch (Exception e)
            {
                var msg = "Unable to fetch simulation status";
                errors.Add(msg);
                this.log.Error(msg, e);
            }

            return simulationRunning;
        }

        // Check the storage dependency status
        private async Task<Tuple<bool, string>> CheckStorageStatusAsync(ICollection<string> errors)
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
                this.log.Error("Storage ping failed", e);
            }

            return result;
        }

        // Check IoT Hub dependency status
        private async Task<Tuple<bool, string>> CheckAzureIoTHubStatusAsync(ICollection<string> errors)
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
                this.log.Error("IoT Hub ping failed", e);
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
    }
}
