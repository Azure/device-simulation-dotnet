// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    public interface IDiagnosticsLogger
    {
        void LogServiceStart(string message);

        void LogServiceHeartbeat();

        void LogServiceError(
            string message,
            string exceptionMessage = "",
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void LogServiceError(
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
        private readonly ILogger log;
        private readonly string diagnosticsEndpoint;

        private const string SERVICE_ERROR_EVENT = "ServiceError";
        private const string SERVICE_START_EVENT = "ServiceStart";
        private const string SERVICE_HEARTBEAT_EVENT = "ServiceHeartbeat";

        public DiagnosticsLogger(IHttpClient httpClient, IServicesConfig servicesConfig, ILogger log)
        {
            this.httpClient = httpClient;
            this.servicesConfig = servicesConfig;
            this.log = log;
            this.diagnosticsEndpoint = this.servicesConfig.DiagnosticsEndpointUrl;
        }

        public void LogServiceStart(string message)
        {
            var jsonStruct = new JsonStruct(SERVICE_START_EVENT + message, null);
            this.PostRequest(this.diagnosticsEndpoint, jsonStruct);
        }

        public void LogServiceHeartbeat()
        {
            var jsonStruct = new JsonStruct(SERVICE_HEARTBEAT_EVENT, null);
            this.PostRequest(this.diagnosticsEndpoint, jsonStruct);
        }

        public void LogServiceError(
            string message,
            string exceptionMessage = "",
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            var jsonStruct = this.ConvertServiceErrorToJson(message, exceptionMessage, null, callerName, filePath, lineNumber);
            this.PostRequest(this.diagnosticsEndpoint, jsonStruct);
        }

        public void LogServiceError(
            string message,
            object data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            var jsonStruct = this.ConvertServiceErrorToJson(message, "", data, callerName, filePath, lineNumber);
            this.PostRequest(this.diagnosticsEndpoint, jsonStruct);
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
            try
            {
                var request = new HttpRequest();
                request.SetUriFromString(path);
                request.SetContent(obj);
                return request;
            }
            catch
            {
                // Failed to construct uri 
                this.log.Info("Failed to construct diagnostics webservice uri ");
                return null;
            }
        }

        private void PostRequest(string path, object obj = null)
        {
            var request = this.PrepareRequest(path, obj);

            if (request != null)
            {
                // Run in the background without blocking
                this.httpClient.PostAsync(request);
            }
        }
    }
}
