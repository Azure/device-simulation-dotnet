// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    class StatusService: IStatusService
    {
        private const string JSON_TRUE = "true";
        private const string JSON_FALSE = "false";

        private const string SIMULATION_RUNNING_KEY = "SimulationRunning";
        private const string PREPROVISIONED_IOTHUB_KEY = "PreprovisionedIoTHub";

        private readonly IPreprovisionedIotHub preprovisionedIotHub;
        private readonly IStorageAdapterClient storageAdapterClient;
        private readonly ISimulations simulations;
        private readonly ILogger log;
        private readonly IServicesConfig servicesConfig;

        public StatusService(
            ILogger logger,
            IPreprovisionedIotHub preprovisionedIotHub,
            IStorageAdapterClient storageAdapterClient,
            ISimulations simulations,
            IServicesConfig servicesConfig
            )
        {
            this.log = logger;
            this.preprovisionedIotHub = preprovisionedIotHub;
            this.simulations = simulations;
            this.storageAdapterClient = storageAdapterClient;
            this.servicesConfig = servicesConfig;
        }

        public async Task<StatusServiceModel> GetStatusAsync()
        {
            var result = new StatusServiceModel(true, "Alive and well!");
            var errors = new List<string>();

            // Simulation status
            var simulationIsRunning = await this.CheckIsSimulationRunningAsync(errors);
            var isRunning = simulationIsRunning.HasValue && simulationIsRunning.Value;

            // Check access to Storage Adapter
            var storageAdapterResult = await this.storageAdapterClient.PingAsync();
            SetServiceStatus("StorgeAdapter", storageAdapterResult, result, errors);

            // Preprovisioned IoT hub status
            var isHubPreprovisioned = this.IsHubConnectionStringConfigured();

            if (isHubPreprovisioned)
            {
                var preprovisionedHubResult = await this.preprovisionedIotHub.PingRegistryAsync();
                SetServiceStatus("IoTHub", preprovisionedHubResult, result, errors);
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
                result.Status.Message = string.Join("; ", errors);
            }

            result.Properties.Add("DiagnosticsEndpointUrl", this.servicesConfig?.DiagnosticsEndpointUrl);
            result.Properties.Add("StorageAdapterApiUrl", this.servicesConfig?.StorageAdapterApiUrl);
            this.log.Info(
                "Service status request",
                () => new {
                    Healthy = result.Status.IsHealthy, result.Status.Message
                });
            return result;
        }

        private void SetServiceStatus(
            string dependencyName,
            StatusResultServiceModel serviceResult,
            StatusServiceModel result,
            List<string> errors
            )
        {
            var StatusResultServiceModel = new StatusResultServiceModel(serviceResult);

            if (!serviceResult.IsHealthy)
            {
                errors.Add(dependencyName + " check failed");
                result.Status.IsHealthy = false;
            }
            result.Dependencies.Add(dependencyName, StatusResultServiceModel);
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
            var cs = this.servicesConfig?.IoTHubConnString?.ToLowerInvariant().Trim();
            return (!string.IsNullOrEmpty(cs)
                    && cs.Contains("hostname=")
                    && cs.Contains("sharedaccesskeyname=")
                    && cs.Contains("sharedaccesskey="));
        }
    }
}
