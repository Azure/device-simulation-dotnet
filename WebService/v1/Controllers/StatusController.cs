// Copyright (c) Microsoft. All rights reserved.

using System.Web.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [RoutePrefix(Version.Name)]
    public sealed class StatusController : ApiController
    {
        public StatusApiModel Get()
        {
            return new StatusApiModel(true, "Alive and well");
        }
    }
}
