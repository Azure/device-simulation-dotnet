// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.PATH + "/[controller]"), ExceptionsFilter]
    public sealed class StatusController : Controller
    {
        private readonly IDevices devices;
        private IStorageAdapterClient storage;
        private readonly ISimulations simulations;
        private readonly ILogger log;

        public StatusController(
            IDevices devices,
            IStorageAdapterClient storage,
            ISimulations simulations,
            ILogger logger)
        {
            this.devices = devices;
            this.storage = storage;
            this.simulations = simulations;
            this.log = logger;
        }

        [HttpGet]
        public async Task<StatusApiModel> Get()
        {
            var statusIsOk = true;
            var statusMsg = "Alive and well";
            var errors = new List<string>();

            // Check access to Azure IoT Hub
            var iotHubStatus = await this.devices.PingRegistryAsync();
            if (!iotHubStatus.Item1)
            {
                statusIsOk = false;
                errors.Add("Unable to use Azure IoT Hub service");
            }

            // Check access to storage
            var storageStatus = await this.storage.PingAsync();
            if (!storageStatus.Item1)
            {
                statusIsOk = false;
                errors.Add("Unable to use Storage");
            }

            // Check simulation status
            bool? simulationRunning = null;
            try
            {
                var simulation = (await this.simulations.GetListAsync()).FirstOrDefault();
                simulationRunning = (simulation != null && simulation.Enabled);
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
            result.Properties.Add("Simulation", simulationRunning.HasValue ? (simulationRunning.Value ? "on" : "off") : "unknown");
            result.Dependencies.Add("IoTHub", iotHubStatus.Item2);
            result.Dependencies.Add("Storage", storageStatus.Item2);

            this.log.Info("Service status request", () => new { Healthy = statusIsOk, statusMsg, running = simulationRunning });

            return result;
        }
    }
}
