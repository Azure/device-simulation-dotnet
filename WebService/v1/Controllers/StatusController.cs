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
        private readonly IDevices devices;
        private readonly IStorageAdapterClient storage;
        private readonly ISimulations simulations;
        private readonly ILogger log;
        private readonly IServicesConfig servicesConfig;

        public StatusController(
            IDevices devices,
            IStorageAdapterClient storage,
            ISimulations simulations,
            ILogger logger,
            IServicesConfig servicesConfig)
        {
            this.devices = devices;
            this.storage = storage;
            this.simulations = simulations;
            this.log = logger;
            this.servicesConfig = servicesConfig;
        }

        [HttpGet]
        public async Task<StatusApiModel> Get()
        {
            var statusIsOk = true;
            var statusMsg = "Alive and well";
            var errors = new List<string>();

            // Check access to Azure IoT Hub
            Tuple<bool, string> iotHubStatus = null;
            try
            {
                if (this.IsHubConnectionStringConfigured())
                {
                    iotHubStatus = await this.devices.PingRegistryAsync();
                    if (!iotHubStatus.Item1)
                    {
                        statusIsOk = false;
                        errors.Add("Unable to use Azure IoT Hub service");
                    }
                }
                else
                {
                    iotHubStatus = new Tuple<bool, string>(false, "not configured");
                }
            }
            catch (Exception e)
            {
                this.log.Error("Hub ping failed", () => new { e });
            }

            // Check access to storage
            Tuple<bool, string> storageStatus = null;
            try
            {
                storageStatus = await this.storage.PingAsync();
                if (!storageStatus.Item1)
                {
                    statusIsOk = false;
                    errors.Add("Unable to use Storage");
                }
            }
            catch (Exception e)
            {
                this.log.Error("Storage ping failed", () => new { e });
            }

            // Check simulation status
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

            // Prepare status message
            if (!statusIsOk)
            {
                statusMsg = string.Join(";", errors);
            }

            // Prepare response
            var result = new StatusApiModel(statusIsOk, statusMsg);

            result.Properties.Add("SimulationRunning", simulationRunning.HasValue ? (simulationRunning.Value ? "true" : "false") : "unknown");
            result.Properties.Add("IoTHubConnectionStringConfigured", this.IsHubConnectionStringConfigured() ? "true" : "false");

            result.Dependencies.Add("IoTHub", iotHubStatus?.Item2);
            result.Dependencies.Add("Storage", storageStatus?.Item2);

            this.log.Info("Service status request", () => new { Healthy = statusIsOk, statusMsg, running = simulationRunning });

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
    }
}
