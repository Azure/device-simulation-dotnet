// Copyright (c) Microsoft. All rights reserved.

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
        public DeviceModelListApiModel Get()
        {
            return DeviceModelListApiModel.FromServiceModel(this.deviceModelsService.GetList());
        }

        [HttpGet("{id}")]
        public DeviceModelApiModel Get(string id)
        {
            return DeviceModelApiModel.FromServiceModel(this.deviceModelsService.Get(id));
        }
    }
}
