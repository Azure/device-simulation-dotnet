// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.Path + "/[controller]"), ExceptionsFilter]
    public sealed class StatusController : Controller
    {
        private readonly IDevices devices;
        private readonly ISimulations simulations;
        private readonly ILogger log;

        public StatusController(
            IDevices devices,
            ISimulations simulations,
            ILogger logger)
        {
            this.devices = devices;
            this.simulations = simulations;
            this.log = logger;
        }

        [HttpGet]
        public async Task<StatusApiModel> Get()
        {
            var statusIsOk = true;
            var statusMsg = "Alive and well";

            var iotHubStatus = await this.devices.PingRegistryAsync();
            if (!iotHubStatus.Item1)
            {
                statusIsOk = false;
                statusMsg = "Unable to use Azure IoT Hub service";
            }

            var simulation = (await this.simulations.GetListAsync()).FirstOrDefault();
            var running = (simulation != null && simulation.Enabled);

            var result = new StatusApiModel(statusIsOk, statusMsg);
            result.Properties.Add("Simulation", running ? "on" : "off");
            result.Dependencies.Add("IoTHub", iotHubStatus.Item2);

            this.log.Info("Service status request", () => new { Healthy = statusIsOk, statusMsg, running });

            return result;
        }
    }
}
