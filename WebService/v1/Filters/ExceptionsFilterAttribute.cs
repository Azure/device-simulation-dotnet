// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
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
    ///
    /// @see https://docs.microsoft.com/aspnet/core/mvc/controllers/filters
    /// </summary>
    public class ExceptionsFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            if (context.Exception is ResourceNotFoundException)
            {
                context.Result = this.GetResponse(HttpStatusCode.NotFound, context.Exception);
            }
            else if (context.Exception is ConflictingResourceException
                     || context.Exception is ResourceOutOfDateException)
            {
                context.Result = this.GetResponse(HttpStatusCode.Conflict, context.Exception);
            }
            else if (context.Exception is BadRequestException
                     || context.Exception is InvalidInputException
                     || context.Exception is Services.Exceptions.InvalidIotHubConnectionStringFormatException
                     || context.Exception is WebService.v1.Exceptions.InvalidIotHubConnectionStringFormatException
                     || context.Exception is IotHubConnectionException)
            {
                context.Result = this.GetResponse(HttpStatusCode.BadRequest, context.Exception);
            }
            else if (context.Exception is InvalidConfigurationException)
            {
                context.Result = this.GetResponse(HttpStatusCode.InternalServerError, context.Exception);
            }
            else if (context.Exception is UnauthorizedAccessException)
            {
                context.Result = this.GetResponse(HttpStatusCode.Forbidden, context.Exception);
            }
            else if (context.Exception != null)
            {
                context.Result = this.GetResponse(HttpStatusCode.InternalServerError, context.Exception, true);
            }
            else
            {
                base.OnException(context);
            }
        }

        public override Task OnExceptionAsync(ExceptionContext context)
        {
            try
            {
                this.OnException(context);
            }
            catch (Exception)
            {
                return base.OnExceptionAsync(context);
            }

            return Task.FromResult(new object());
        }

        private ObjectResult GetResponse(
            HttpStatusCode code,
            Exception e,
            bool stackTrace = false)
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

            var result = new ObjectResult(error);
            result.StatusCode = (int) code;
            result.Formatters.Add(new JsonOutputFormatter(new JsonSerializerSettings(), ArrayPool<char>.Shared));

            return result;
        }
    }
}
