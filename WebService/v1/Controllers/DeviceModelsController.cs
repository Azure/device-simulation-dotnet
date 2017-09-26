// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

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
            return new DeviceModelListApiModel(this.deviceModelsService.GetList());
        }

        [HttpGet("{id}")]
        public DeviceModelApiModel Get(string id)
        {
            return new DeviceModelApiModel(this.deviceModelsService.Get(id));
        }
    }
}
