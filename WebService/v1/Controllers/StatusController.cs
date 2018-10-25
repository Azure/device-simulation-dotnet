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
        private readonly IStorageAdapterClient storageAdapterClient;
        private readonly ISimulations simulations;
        private readonly ILogger log;
        private readonly IServicesConfig servicesConfig;

        public StatusController(
            IPreprovisionedIotHub preprovisionedIotHub,
            IStorageAdapterClient storageAdapterClient,
            ISimulations simulations,
            ILogger logger,
            IServicesConfig servicesConfig)
        {
            this.preprovisionedIotHub = preprovisionedIotHub;
            this.storageAdapterClient = storageAdapterClient;
            this.simulations = simulations;
            this.log = logger;
            this.servicesConfig = servicesConfig;
        }

        // TODO: reduce method complexity, refactor out some logic
        [HttpGet]
        public async Task<StatusApiModel> GetAsync()
        {
            var statusIsOk = true;
            var statusMsg = SERVICE_IS_HEALTHY;
            var errors = new List<string>();

            StatusModel storageAdapterStatusModel = new StatusModel();
            StatusModel ioTHubStatusModel = new StatusModel();

            // Simulation status
            var simulationIsRunning = await this.CheckIsSimulationRunningAsync(errors);
            var isRunning = simulationIsRunning.HasValue && simulationIsRunning.Value;

            // Check access to Storage Adapter
            var storageAdapterStatus = await this.storageAdapterClient.PingAsync();
            if (storageAdapterStatus != null)
            {
                if (!storageAdapterStatus.Item1)
                {
                    statusIsOk = false;
                    var message = "Unable to connect to Storage Adapter service";
                    errors.Add(message);
                    storageAdapterStatusModel.Message = message;
                    storageAdapterStatusModel.IsConnected = false;
                }
                else
                {
                    storageAdapterStatusModel.Message = storageAdapterStatus.Item2;
                    storageAdapterStatusModel.IsConnected = true;
                }
            }

            // Preprovisioned IoT hub status
            var isHubPreprovisioned = this.IsHubConnectionStringConfigured();

            if (isHubPreprovisioned)
            {
                var preprovisionedHubStatus = await this.CheckAzureIoTHubStatusAsync(errors);
                if (preprovisionedHubStatus != null)
                {
                    if (!preprovisionedHubStatus.Item1)
                    {
                        statusIsOk = false;
                        var message = "Unable to connect to IoTHUb";
                        errors.Add(message);
                        ioTHubStatusModel.Message = message;
                        ioTHubStatusModel.IsConnected = false;
                    }
                    else
                    {
                        ioTHubStatusModel.Message = preprovisionedHubStatus.Item2;
                        ioTHubStatusModel.IsConnected = true;
                    }
                }
            }

            // Prepare status message and response
            if (!statusIsOk)
            {
                statusMsg = string.Join(";", errors);
            }

            // Prepare response
            var result = new StatusApiModel(statusIsOk, statusMsg);
            result.Properties.Add(SIMULATION_RUNNING_KEY,
                simulationIsRunning.HasValue
                    ? (isRunning ? JSON_TRUE : JSON_FALSE)
                    : "unknown");
            result.Properties.Add(PREPROVISIONED_IOTHUB_KEY,
                isHubPreprovisioned
                    ? JSON_TRUE
                    : JSON_FALSE);

            result.Dependencies.Add("Storage", storageAdapterStatusModel);
            if (isHubPreprovisioned)
            {
                result.Dependencies.Add(PREPROVISIONED_IOTHUB_KEY, ioTHubStatusModel);
            }

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
