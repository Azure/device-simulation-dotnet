// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters
{
    public class ExceptionsFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            if (context.Exception is ConflictingResourceException)
            {
                context.Response = new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    ReasonPhrase = context.Exception.Message
                };
                return;
            }

            if (context.Exception is ResourceOutOfDateException)
            {
                context.Response = new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    ReasonPhrase = context.Exception.Message
                };
                return;
            }

            if (context.Exception is ResourceNotFoundException)
            {
                context.Response = new HttpResponseMessage(HttpStatusCode.NotFound);
                return;
            }

            if (context.Exception is InvalidConfigurationException)
            {
                context.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = context.Exception.Message
                };
                return;
            }
        }
    }
}
