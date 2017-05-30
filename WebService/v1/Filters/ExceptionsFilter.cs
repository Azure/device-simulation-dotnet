// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters
{
    /// <summary>
    /// Detect all the unhandled exceptions returned by the API controllers
    /// and decorate the response accordingly, managing the HTTP status code
    /// and preparing a JSON response with useful error details.
    /// When including the stack trace, split the text in multiple lines
    /// for an easier parsing.
    /// </summary>
    public class ExceptionsFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            if (context.Exception is ResourceNotFoundException)
            {
                context.Response = this.GetResponse(HttpStatusCode.NotFound, context.Exception);
            }
            else if (context.Exception is ConflictingResourceException
                     || context.Exception is ResourceOutOfDateException)
            {
                context.Response = this.GetResponse(HttpStatusCode.Conflict, context.Exception);
            }
            else if (context.Exception is BadRequestException
                     || context.Exception is InvalidInputException)
            {
                context.Response = this.GetResponse(HttpStatusCode.BadRequest, context.Exception);
            }
            else if (context.Exception is InvalidConfigurationException)
            {
                context.Response = this.GetResponse(HttpStatusCode.InternalServerError, context.Exception);
            }
            else if (context.Exception != null)
            {
                context.Response = this.GetResponse(HttpStatusCode.InternalServerError, context.Exception, true);
            }
            else
            {
                base.OnException(context);
            }
        }

        public override Task OnExceptionAsync(HttpActionExecutedContext context, CancellationToken token)
        {
            try
            {
                this.OnException(context);
            }
            catch (Exception)
            {
                return base.OnExceptionAsync(context, token);
            }

            return Task.FromResult(new VoidTask());
        }

        private struct VoidTask
        {
        }

        private HttpResponseMessage GetResponse(HttpStatusCode code, Exception e, bool stackTrace = false)
        {
            var error = new Dictionary<string, object>
            {
                ["Message"] = "An error has occurred.",
                ["ExceptionMessage"] = e.Message,
                ["ExceptionType"] = e.GetType().FullName
            };

            if (stackTrace)
            {
                error["StackTrace"] = e.StackTrace.Split(new[] { "\n" }, StringSplitOptions.None);

                if (e.InnerException != null)
                {
                    e = e.InnerException;
                    error["InnerExceptionMessage"] = e.Message;
                    error["InnerExceptionType"] = e.GetType().FullName;
                    error["InnerExceptionStackTrace"] = e.StackTrace.Split(new[] { "\n" }, StringSplitOptions.None);
                }
            }

            return new HttpResponseMessage(code)
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(error),
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
