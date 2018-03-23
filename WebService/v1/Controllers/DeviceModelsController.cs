// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.PATH + "/[controller]"), ExceptionsFilter]
    public class DeviceModelsController : Controller
    {
        private readonly IDeviceModels deviceModelsService;

        public DeviceModelsController(IDeviceModels deviceModelsService)
        {
            this.deviceModelsService = deviceModelsService;
        }

        [HttpGet]
        public async Task<DeviceModelListApiModel> GetAsync()
        {
            return DeviceModelListApiModel.FromServiceModel(await this.deviceModelsService.GetListAsync());
        }

        [HttpGet("{id}")]
        public async Task<DeviceModelApiModel> GetAsync(string id)
        {
            return DeviceModelApiModel.FromServiceModel(await this.deviceModelsService.GetAsync(id));
        }
    }
}
