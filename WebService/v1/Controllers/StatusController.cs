// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.Path + "/[controller]"), ExceptionsFilter]
    public sealed class StatusController : Controller
    {
        private readonly IIoTHubManager ioTHubManager;
        private readonly ISimulations simulations;
        private readonly IConfig config;
        private readonly ILogger log;

        public StatusController(
            IIoTHubManager ioTHubManager,
            ISimulations simulations,
            IConfig config,
            ILogger logger)
        {
            this.ioTHubManager = ioTHubManager;
            this.simulations = simulations;
            this.config = config;
            this.log = logger;
        }

        [HttpGet]
        public async Task<StatusApiModel> Get()
        {
            var statusIsOk = true;
            var statusMsg = "Alive and well";

            var iotHubManagerStatus = await this.ioTHubManager.PingAsync();
            if (!iotHubManagerStatus.Item1)
            {
                statusIsOk = false;
                statusMsg = "Unable to use IoT Hub Manager web service";
            }

            var simulation = this.simulations.GetList().FirstOrDefault();
            var running = (simulation != null && simulation.Enabled);

            var result = new StatusApiModel(statusIsOk, statusMsg);
            result.Properties.Add("Simulation", running ? "on" : "off");
            result.Properties.Add("IoTHubManagerUrl", this.config.ServicesConfig.IoTHubManagerApiUrl);
            result.Properties.Add("IoTHubManagerTimeout", this.config.ServicesConfig.IoTHubManagerApiTimeout.ToString());
            result.Dependencies.Add("IoTHubManager", iotHubManagerStatus.Item2);

            this.log.Info("Service status request", () => new { Healthy = statusIsOk, statusMsg, running });

            return result;
        }
    }
}
