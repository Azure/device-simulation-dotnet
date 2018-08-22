// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{

    public interface IDiagnosticsLogger
    {
        Task<IHttpResponse> LogServiceStartAsync();
        Task<IHttpResponse> LogServiceHeartbeatAsync();
        Task<IHttpResponse> LogServiceErrorAsync(
            string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);
        Task<IHttpResponse> LogServiceErrorAsync(
            string message,
            Exception e,
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
            public string EventId;
            public string EventType;
            public string DeploymentId;
            public string SolutionType;
            public Dictionary<string, object> EventProperties;

            public JsonStruct(string st1, string st2, string st3, string st4, Dictionary<string, object> eventProps)
            {
                EventId = st1;
                EventType = st2;
                DeploymentId = st3;
                SolutionType = st4;
                EventProperties = eventProps;
            }
        }

        private readonly IHttpClient httpClient;
        private readonly IServicesConfig servicesConfig;

        private const string SERVICE_ERROR_EVENT = "ServiceError";
        private const string SERVICE_START_EVENT = "ServiceStart";
        private const string SERVICE_HEARTBEAT_EVENT = "ServiceHeartbeat";
        private string DEPLOYMENT_ID = "";
        private string SOLUTION_TYPE = "";
              
        public DiagnosticsLogger(IHttpClient httpClient, IServicesConfig servicesConfig)
        {
            this.httpClient = httpClient;
            this.servicesConfig = servicesConfig;
            DEPLOYMENT_ID = this.servicesConfig.DeploymentId;
            SOLUTION_TYPE = this.servicesConfig.SolutionType;
        }

        public async Task<IHttpResponse> LogServiceStartAsync()
        {
            JsonStruct jsonStruct = new JsonStruct(Guid.NewGuid().ToString(),
                                                SERVICE_START_EVENT,
                                                DEPLOYMENT_ID,
                                                SOLUTION_TYPE,
                                                null);

            return await httpClient.PostAsync(this.PrepareRequest(this.servicesConfig.DiagnosticsEndpointUrl, jsonStruct));
        }

        public async Task<IHttpResponse> LogServiceHeartbeatAsync()
        {
            JsonStruct jsonStruct = new JsonStruct(Guid.NewGuid().ToString(),
                                                SERVICE_HEARTBEAT_EVENT,
                                                DEPLOYMENT_ID,
                                                SOLUTION_TYPE,
                                                null);

            return await httpClient.PostAsync(this.PrepareRequest(this.servicesConfig.DiagnosticsEndpointUrl, jsonStruct));
        }

        public async Task<IHttpResponse> LogServiceErrorAsync(
            string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            JsonStruct jsonStruct = this.ConvertToJson(message, null, null, callerName, filePath, lineNumber);
            return await httpClient.PostAsync(this.PrepareRequest(this.servicesConfig.DiagnosticsEndpointUrl, jsonStruct));
        }

        public async Task<IHttpResponse> LogServiceErrorAsync(
            string message,
            Exception e,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            JsonStruct jsonStruct = this.ConvertToJson(message, e, null, callerName, filePath, lineNumber);
            return await httpClient.PostAsync(this.PrepareRequest(this.servicesConfig.DiagnosticsEndpointUrl, jsonStruct));
        }

        public async Task<IHttpResponse> LogServiceErrorAsync(
            string message,
            object data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            JsonStruct jsonStruct = this.ConvertToJson(message, null, data, callerName, filePath, lineNumber);
            return await httpClient.PostAsync(this.PrepareRequest(this.servicesConfig.DiagnosticsEndpointUrl, jsonStruct));
        }

        private JsonStruct ConvertToJson(string message,
            Exception e,
            object data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Dictionary<string, object> eventProps = new Dictionary<string, object>();
            eventProps.Add("message", message + $"(CallerMemberName = '{callerName}', CallerFilePath = '{filePath}', lineNumber = '{lineNumber}')");
            if (e != null)
            {
                eventProps.Add("Exception", e);
            }
            if (data != null)
            {
                eventProps.Add("data", data);
            }
            var EventId = Guid.NewGuid().ToString();
            return new JsonStruct(Guid.NewGuid().ToString(), "ServiceError", DEPLOYMENT_ID, SOLUTION_TYPE, eventProps);
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
