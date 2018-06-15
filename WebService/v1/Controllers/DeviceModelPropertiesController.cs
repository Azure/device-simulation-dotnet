// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.PATH + "/[controller]"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class DeviceModelPropertiesController : Controller
    {
        private readonly IDeviceModels deviceModelsService;

        public DeviceModelPropertiesController(IDeviceModels deviceModelsService)
        {
            this.deviceModelsService = deviceModelsService;
        }

        [HttpGet]
        public async Task<DeviceModelPropertyListApiModel> GetAsync()
        {
            return new DeviceModelPropertyListApiModel(await this.deviceModelsService.GetPropertyNamesAsync());
        }
    }
}
