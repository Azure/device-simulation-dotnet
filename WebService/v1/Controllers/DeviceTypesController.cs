// Copyright (c) Microsoft. All rights reserved.

using System.Web.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;
using Microsoft.Web.Http;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [ApiVersion(Version.Number), ExceptionsFilter]
    public class DeviceTypesController : ApiController
    {
        private readonly IDeviceTypes deviceTypesService;

        public DeviceTypesController(IDeviceTypes deviceTypesService)
        {
            this.deviceTypesService = deviceTypesService;
        }

        public DeviceTypeListApiModel Get()
        {
            return new DeviceTypeListApiModel(this.deviceTypesService.GetList());
        }

        public DeviceTypeApiModel Get(string id)
        {
            return new DeviceTypeApiModel(this.deviceTypesService.Get(id));
        }
    }
}
