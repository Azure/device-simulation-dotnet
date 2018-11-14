// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.PATH + "/[controller]"), ExceptionsFilter]
    public class DevicesController : Controller
    {
        private ISimulations simulationsService;

        public DevicesController(ISimulations simulationsService)
        {
            this.simulationsService = simulationsService;
        }

        [HttpGet]
        public async Task<DeviceListApiModel> GetAynsc(
            [FromQuery(Name = "skip")] string skip = "",
            [FromQuery(Name = "limit")] string limit = "1000"
            )
        {
            var list = await this.simulationsService.GetDeviceList(skip, limit);

            return DeviceListApiModel.FromServiceModel(list);
        }
    }
}
