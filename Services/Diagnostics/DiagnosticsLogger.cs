// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{

    public interface IDiagnosticsLogger
    {
        Task<IHttpResponse> LogServiceStartAsync(string message);

        Task<IHttpResponse> LogServiceHeartbeatAsync();

        Task<IHttpResponse> LogServiceErrorAsync(
            string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        Task<IHttpResponse> LogServiceErrorAsync(
            string message,
            object data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        Task<IHttpResponse> LogServiceExceptionAsync(
            string message,
            string exceptionMessage,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);
    }

    public class DiagnosticsLogger : IDiagnosticsLogger
    {
        public struct JsonStruct
        {
            public string EventType;
            public Dictionary<string, object> EventProperties;

            public JsonStruct(string eventType, Dictionary<string, object> eventProps)
            {
                EventType = eventType;
                EventProperties = eventProps;
            }
        }

        private readonly IHttpClient httpClient;
        private readonly IServicesConfig servicesConfig;

        private const string SERVICE_ERROR_EVENT = "ServiceError";
        private const string SERVICE_START_EVENT = "ServiceStart";
        private const string SERVICE_HEARTBEAT_EVENT = "ServiceHeartbeat";
              
        public DiagnosticsLogger(IHttpClient httpClient, IServicesConfig servicesConfig)
        {
            this.httpClient = httpClient;
            this.servicesConfig = servicesConfig;
        }

        public async Task<IHttpResponse> LogServiceStartAsync(string message)
        {
            JsonStruct jsonStruct = new JsonStruct(SERVICE_START_EVENT + message, null);

            return await httpClient.PostAsync(this.PrepareRequest(this.servicesConfig.DiagnosticsEndpointUrl, jsonStruct));
        }

        public async Task<IHttpResponse> LogServiceHeartbeatAsync()
        {
            JsonStruct jsonStruct = new JsonStruct(SERVICE_HEARTBEAT_EVENT,null);

            return await httpClient.PostAsync(this.PrepareRequest(this.servicesConfig.DiagnosticsEndpointUrl, jsonStruct));
        }

        public async Task<IHttpResponse> LogServiceErrorAsync(
            string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            JsonStruct jsonStruct = this.ConvertToJson(message, "", null, callerName, filePath, lineNumber);
            return await httpClient.PostAsync(this.PrepareRequest(this.servicesConfig.DiagnosticsEndpointUrl, jsonStruct));
        }

        public async Task<IHttpResponse> LogServiceExceptionAsync(
            string message,
            string exceptionMessage,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            JsonStruct jsonStruct = this.ConvertToJson(message, exceptionMessage, null, callerName, filePath, lineNumber);
            return await httpClient.PostAsync(this.PrepareRequest(this.servicesConfig.DiagnosticsEndpointUrl, jsonStruct));
        }

        public async Task<IHttpResponse> LogServiceErrorAsync(
            string message,
            object data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            JsonStruct jsonStruct = this.ConvertToJson(message, "", data, callerName, filePath, lineNumber);
            return await httpClient.PostAsync(this.PrepareRequest(this.servicesConfig.DiagnosticsEndpointUrl, jsonStruct));
        }

        private JsonStruct ConvertToJson(string message,
            string exceptionMessage,
            object data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Dictionary<string, object> eventProps = new Dictionary<string, object>();
            eventProps.Add("message", message + $"(CallerMemberName = '{callerName}', CallerFilePath = '{filePath}', lineNumber = '{lineNumber}')");
            if (!string.IsNullOrEmpty(exceptionMessage))
            {
                eventProps.Add("ExceptionMessage", exceptionMessage);
            }
            if (data != null)
            {
                eventProps.Add("data", data);
            }
            return new JsonStruct(SERVICE_ERROR_EVENT, eventProps);
        }

        private HttpRequest PrepareRequest(string path, object obj = null)
        {
            var request = new HttpRequest();
            request.SetUriFromString(path);
            request.SetContent(obj);
            return request;
        }
    }
}
