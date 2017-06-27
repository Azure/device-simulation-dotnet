// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

// TODO: complete
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.Path + "/[controller]"), ExceptionsFilter]
    public sealed class StatusController : Controller
    {
        private readonly ISimulations simulations;
        private readonly IConfig config;
        private readonly ILogger log;

        public StatusController(
            ISimulations simulations,
            IConfig config,
            ILogger logger)
        {
            this.simulations = simulations;
            this.config = config;
            this.log = logger;
        }

        [HttpGet]
        public StatusApiModel Get()
        {
            // TODO: calculate the actual service status
            var isOk = true;

            var simulation = this.simulations.GetList().FirstOrDefault();
            var running = (simulation != null && simulation.Enabled);

            this.log.Info("Service status request", () => new { Healthy = isOk, running });

            var result = new StatusApiModel(isOk, "Alive and well");

            result.Properties.Add("Simulation", running ? "on" : "off");
            result.Properties.Add("IoTHubManagerUrl", this.config.ServicesConfig.IoTHubManagerApiUrl);
            result.Properties.Add("IoTHubManagerTimeout", this.config.ServicesConfig.IoTHubManagerApiTimeout.ToString());

            return result;
        }
    }
}
