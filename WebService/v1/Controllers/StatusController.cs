// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.PATH + "/[controller]"), ExceptionsFilter]
    public sealed class StatusController : Controller
    {
        private const string JSON_TRUE = "true";
        private const string JSON_FALSE = "false";

        private const string SIMULATION_RUNNING_KEY = "SimulationRunning";
        private const string PREPROVISIONED_IOTHUB_KEY = "PreprovisionedIoTHub";

        private readonly IPreprovisionedIotHub preprovisionedIotHub;
        private readonly IStorageAdapterClient storageAdapterClient;
        private readonly ISimulations simulations;
        private readonly ILogger log;
        private readonly IConfig config;

        public StatusController(
            IPreprovisionedIotHub preprovisionedIotHub,
            IStorageAdapterClient storageAdapterClient,
            ISimulations simulations,
            ILogger logger,
            IConfig config)
        {
            this.preprovisionedIotHub = preprovisionedIotHub;
            this.storageAdapterClient = storageAdapterClient;
            this.simulations = simulations;
            this.log = logger;
            this.config = config;
        }

        // TODO: reduce method complexity, refactor out some logic
        [HttpGet]
        public async Task<StatusApiModel> GetAsync()
        {
            var result = new StatusApiModel();
            var errors = new List<string>();

            // Simulation status
            var simulationIsRunning = await this.CheckIsSimulationRunningAsync(errors);
            var isRunning = simulationIsRunning.HasValue && simulationIsRunning.Value;

            // Check access to Storage Adapter
            var storageAdapterTuple = await this.storageAdapterClient.PingAsync();
            SetServiceStatus("StorgeAdapter", storageAdapterTuple, result, errors);

            // Preprovisioned IoT hub status
            var isHubPreprovisioned = this.IsHubConnectionStringConfigured();

            if (isHubPreprovisioned)
            {
                var preprovisionedHubTuple = await this.preprovisionedIotHub.PingRegistryAsync();
                SetServiceStatus("IoTHub", preprovisionedHubTuple, result, errors);
            }

            result.Properties.Add(SIMULATION_RUNNING_KEY,
                simulationIsRunning.HasValue
                    ? (isRunning ? JSON_TRUE : JSON_FALSE)
                    : "unknown");
            result.Properties.Add(PREPROVISIONED_IOTHUB_KEY,
                isHubPreprovisioned
                    ? JSON_TRUE
                    : JSON_FALSE);

            if (errors.Count > 0)
            {
                result.Message = string.Join("; ", errors);
            }

            result.Properties.Add("DiagnosticsEndpointUrl", this.config.ServicesConfig?.DiagnosticsEndpointUrl);
            result.Properties.Add("StorageAdapterApiUrl", this.config.ServicesConfig?.StorageAdapterApiUrl);
            result.Properties.Add("Port", this.config.Port.ToString());
            this.log.Info("Service status request", () => new { Healthy = result.IsHealthy, result.Message });
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

        // Check whether the configuration contains a connection string
        private bool IsHubConnectionStringConfigured()
        {
            var cs = this.config.ServicesConfig?.IoTHubConnString?.ToLowerInvariant().Trim();
            return (!string.IsNullOrEmpty(cs)
                    && cs.Contains("hostname=")
                    && cs.Contains("sharedaccesskeyname=")
                    && cs.Contains("sharedaccesskey="));
        }

        private void SetServiceStatus(
            string dependencyName,
            Tuple<bool, string> serviceTuple,
            StatusApiModel result,
            List<string> errors
            )
        {
            if (serviceTuple == null)
            {
                return;
            }
            var serviceStatusModel = new StatusModel
            {
                Message = serviceTuple.Item2,
                IsHealthy = serviceTuple.Item1
            };

            if (!serviceTuple.Item1)
            {
                errors.Add(dependencyName + " check failed");
                result.IsHealthy = serviceTuple.Item1;
            }
            result.Dependencies.Add(dependencyName, serviceStatusModel);
        }
    }
}
