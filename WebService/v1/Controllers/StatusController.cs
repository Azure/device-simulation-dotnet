// Copyright (c) Microsoft. All rights reserved.

using System.Web.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [RoutePrefix(Version.Name)]
    public class StatusController : ApiController
    {
        /// <summary>Return the service status</summary>
        /// <returns>Status object</returns>
        public StatusModel Get()
        {
            return new StatusModel { Message = "OK" };
        }
    }
}
