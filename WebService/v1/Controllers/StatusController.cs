// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.PATH + "/[controller]"), ExceptionsFilter]
    public sealed class StatusController : Controller
    {
        private readonly IConfig config;
        private readonly IStatusService statusService;

        public StatusController(IConfig config, IStatusService statusService)
        {
            this.statusService = statusService;
            this.config = config;
        }

        // TODO: reduce method complexity, refactor out some logic
        [HttpGet]
        public async Task<StatusApiModel> GetAsync()
        {
            var result = new StatusApiModel(await this.statusService.GetStatusAsync());

            result.Properties.Add("Port", this.config.Port.ToString());
            return result;
        }
    }
}
