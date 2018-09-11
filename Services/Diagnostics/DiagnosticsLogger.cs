// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    public interface IDiagnosticsLogger
    {
        Task<IHttpResponse> LogServiceStartAsync(string message);

        Task<IHttpResponse> LogServiceHeartbeatAsync();

        Task<IHttpResponse> LogServiceErrorAsync(
            string message,
            string exceptionMessage = "",
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        Task<IHttpResponse> LogServiceErrorAsync(
            string message,
            object data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);
    }

    public class DiagnosticsLogger : IDiagnosticsLogger
    {
        public struct JsonStruct
        {
            [JsonProperty(PropertyName = "EventType")]
            public string EventType;

            [JsonProperty(PropertyName = "EventProperties")]
            public Dictionary<string, object> EventProperties;

            public JsonStruct(string eventType, Dictionary<string, object> eventProps)
            {
                this.EventType = eventType;
                this.EventProperties = eventProps;
            }
        }

        private readonly IHttpClient httpClient;
        private readonly IServicesConfig servicesConfig;
        private readonly string diagnosticsEndpoint = string.Empty;

        private const string SERVICE_ERROR_EVENT = "ServiceError";
        private const string SERVICE_START_EVENT = "ServiceStart";
        private const string SERVICE_HEARTBEAT_EVENT = "ServiceHeartbeat";

        public DiagnosticsLogger(IHttpClient httpClient, IServicesConfig servicesConfig)
        {
            this.httpClient = httpClient;
            this.servicesConfig = servicesConfig;
            this.diagnosticsEndpoint = this.servicesConfig.DiagnosticsEndpointUrl + "/diagnosticsevents";
        }

        public async Task<IHttpResponse> LogServiceStartAsync(string message)
        {
            JsonStruct jsonStruct = new JsonStruct(SERVICE_START_EVENT + message, null);
            return await this.httpClient.PostAsync(this.PrepareRequest(this.diagnosticsEndpoint, jsonStruct));
        }

        public async Task<IHttpResponse> LogServiceHeartbeatAsync()
        {
            JsonStruct jsonStruct = new JsonStruct(SERVICE_HEARTBEAT_EVENT, null);
            return await this.httpClient.PostAsync(this.PrepareRequest(this.diagnosticsEndpoint, jsonStruct));
        }

        public async Task<IHttpResponse> LogServiceErrorAsync(
            string message,
            string exceptionMessage = "",
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            JsonStruct jsonStruct = this.ConvertServiceErrorToJson(message, exceptionMessage, null, callerName, filePath, lineNumber);
            return await this.httpClient.PostAsync(this.PrepareRequest(this.diagnosticsEndpoint, jsonStruct));
        }

        public async Task<IHttpResponse> LogServiceErrorAsync(
            string message,
            object data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            JsonStruct jsonStruct = this.ConvertServiceErrorToJson(message, "", data, callerName, filePath, lineNumber);
            return await this.httpClient.PostAsync(this.PrepareRequest(this.diagnosticsEndpoint, jsonStruct));
        }

        private JsonStruct ConvertServiceErrorToJson(string message,
            string exceptionMessage,
            object data,
            string callerName = "",
            string filePath = "",
            int lineNumber = 0)
        {
            var eventProps = new Dictionary<string, object>
            {
                { "message", message + $"(CallerMemberName = '{callerName}', CallerFilePath = '{filePath}', lineNumber = '{lineNumber}')" }
            };

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
