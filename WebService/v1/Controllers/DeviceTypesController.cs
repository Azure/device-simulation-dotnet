// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.Path + "/[controller]"), ExceptionsFilter]
    public class DeviceTypesController : Controller
    {
        private readonly IDeviceTypes deviceTypesService;

        public DeviceTypesController(IDeviceTypes deviceTypesService)
        {
            this.deviceTypesService = deviceTypesService;
        }

        [HttpGet]
        public DeviceTypeListApiModel Get()
        {
            return new DeviceTypeListApiModel(this.deviceTypesService.GetList());
        }

        [HttpGet("{id}")]
        public DeviceTypeApiModel Get(string id)
        {
            return new DeviceTypeApiModel(this.deviceTypesService.Get(id));
        }
    }
}
