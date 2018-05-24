// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.PATH + "/[controller]"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class DevicePropertiesController : Controller
    {
        private readonly IDeviceModels deviceModelsService;

        public DevicePropertiesController(IDeviceModels deviceModelsService)
        {
            this.deviceModelsService = deviceModelsService;
        }

        [HttpGet]
        public async Task<DevicePropertiesApiModel> GetDevicePropertiesAsync()
        {
            return new DevicePropertiesApiModel(await this.deviceModelsService.GetPropertyNamesAsync());
        }
    }
}
