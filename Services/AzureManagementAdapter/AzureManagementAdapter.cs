// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.AzureManagementAdapter
{
    public interface IAzureManagementAdapterClient
    {
        Task<MetricsResponseModel> PostAsync(MetricsRequestsModel requests);
    }

    public class AzureManagementAdapter : IAzureManagementAdapterClient
    {
        private const string DATE_FORMAT = "yyyy-MM-ddTHH:mm:ssZ";
        private const string METRICS_API_VERSION = "2016-06-01";
        private readonly IServicesConfig config;
        private readonly IDeploymentConfig deploymentConfig;
        private readonly IDiagnosticsLogger diagnosticsLogger;
        private readonly IHttpClient httpClient;
        private readonly ILogger log;
        private SecureString secureAccessToken = new SecureString();
        private DateTimeOffset tokenExpireTime = DateTimeOffset.UtcNow;

        public AzureManagementAdapter(
            IHttpClient httpClient,
            IServicesConfig config,
            IDeploymentConfig deploymentConfig,
            ILogger logger,
            IDiagnosticsLogger diagnosticsLogger)
        {
            this.httpClient = httpClient;
            this.log = logger;
            this.config = config;
            this.deploymentConfig = deploymentConfig;
            this.diagnosticsLogger = diagnosticsLogger;
        }

        /// <summary>
        ///     Query Azure Managemet API for the IotHub metrics.
        ///     More details in following docs:
        ///     https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-metrics
        ///     https://docs.microsoft.com/en-us/rest/api/monitor/metrics/list
        /// </summary>
        /// <param name="token"></param>
        /// <param name="requests"></param>
        /// <returns></returns>
        public async Task<MetricsResponseModel> PostAsync(MetricsRequestsModel requests)
        {
            if (this.AccessTokenIsNullOrEmpty()) await this.GetAadTokenAsync();

            if (this.AccessTokenExpireSoon()) this.GetAadTokenAsync();

            if (requests == null) requests = this.GetDefaultMetricsRequests();

            var accessToken = $"Bearer {this.ReadSecureString(this.secureAccessToken)}";

            var request = this.PrepareRequest($"batch?api-version={this.config.AzureManagementAdapterApiVersion}", accessToken, requests);

            this.log.Debug("Azure Management request content", () => new { request.Content });

            var response = await this.httpClient.PostAsync(request);

            this.log.Debug("Azure Management response", () => new { response });

            this.ThrowIfError(response);

            return JsonConvert.DeserializeObject<MetricsResponseModel>(response.Content);
        }

        private bool AccessTokenIsNullOrEmpty()
        {
            return this.secureAccessToken.Length == 0;
        }

        private bool AccessTokenExpireSoon()
        {
            return this.tokenExpireTime.AddMinutes(-10) < DateTimeOffset.UtcNow;
        }

        private HttpRequest PrepareRequest(string path, string token, MetricsRequestsModel content = null)
        {
            var request = new HttpRequest();
            request.AddHeader(HttpRequestHeader.Accept.ToString(), "application/json");
            request.AddHeader(HttpRequestHeader.CacheControl.ToString(), "no-cache");
            request.AddHeader(HttpRequestHeader.Authorization.ToString(), token);
            request.SetUriFromString($"{this.config.AzureManagementAdapterApiUrl}/{path}");
            request.Options.EnsureSuccess = false;
            request.Options.Timeout = this.config.AzureManagementAdapterApiTimeout;
            if (!this.config.AzureManagementAdapterApiUrl.ToLowerInvariant().StartsWith("https:")) throw new InvalidConfigurationException("Azure Management API url must start with https");

            if (content != null) request.SetContent(content);

            this.log.Debug("Azure Management request", () => new { request });

            return request;
        }

        private void ThrowIfError(IHttpResponse response)
        {
            if (!response.IsError) return;

            this.log.Error("Metrics request error", () => new { response.Content });
            this.diagnosticsLogger.LogServiceError("Metrics request error", new { response.Content });
            throw new ExternalDependencyException(
                new HttpRequestException($"Metrics request error: status code {response.StatusCode}"));
        }

        private string GetDefaultIoTHubMetricsUrl()
        {
            return $"/subscriptions/{this.deploymentConfig.AzureSubscriptionId}" +
                   $"/resourceGroups/{this.deploymentConfig.AzureResourceGroup}" +
                   $"/providers/Microsoft.Devices/IotHubs/{this.deploymentConfig.AzureIothubName}" +
                   $"/providers/Microsoft.Insights/metrics?api-version={METRICS_API_VERSION}&" +
                   $"$filter={this.GetDefaultMetricsQuery()}";
        }

        private async Task GetAadTokenAsync()
        {
            var request = new HttpRequest();
            request.AddHeader(HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded");

            var values = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "resource", this.config.AzureManagementAdapterApiUrl },
                { "client_id", this.deploymentConfig.AadAppId },
                { "client_secret", this.deploymentConfig.AadAppSecret }
            };
            var content = new FormUrlEncodedContent(values);

            request.SetUriFromString($"{this.deploymentConfig.AadTokenUrl}/{this.deploymentConfig.AadTenantId}/oauth2/token");
            request.Options.EnsureSuccess = false;
            request.Options.Timeout = this.config.AzureManagementAdapterApiTimeout;
            request.SetContent(content);

            var response = await this.httpClient.PostAsync(request);

            this.ThrowIfError(response);

            var tokenModel = JsonConvert.DeserializeObject<AadTokenModel>(response.Content);

            this.secureAccessToken = this.GetSecureAccessToken(tokenModel.AccessToken);
            this.tokenExpireTime = DateTimeOffset.FromUnixTimeSeconds(int.Parse(tokenModel.ExpiresOn));
        }

        private SecureString GetSecureAccessToken(string accessToken)
        {
            if (accessToken == null) return null;

            var secureString = new SecureString();
            foreach (var c in accessToken) secureString.AppendChar(c);

            return secureString;
        }

        private string ReadSecureString(SecureString secureToken)
        {
            var rawToken = IntPtr.Zero;
            try
            {
                rawToken = Marshal.SecureStringToGlobalAllocUnicode(secureToken);
                return Marshal.PtrToStringUni(rawToken);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(rawToken);
            }
        }

        /// <summary>
        ///     Return the default query for Azure management API.
        ///     Data points:
        ///     1) Total devices
        ///     2) Connected devices
        ///     3) Telemetry message sent
        ///     Time range: Last one hour
        ///     Time Grain: 1 minite
        /// </summary>
        /// <returns></returns>
        private string GetDefaultMetricsQuery()
        {
            // TODO: consider add support for query params from controller
            var now = DateTimeOffset.UtcNow;
            var startTime = now.AddHours(-1).ToString(DATE_FORMAT);
            var endTime = now.ToString(DATE_FORMAT);

            string[] nameArray =
            {
                "name.value eq 'devices.connectedDevices.allProtocol'",
                "name.value eq 'd2c.telemetry.ingress.success'",
                "name.value eq 'devices.totalDevices'"
            };

            string[] typeArray =
            {
                "aggregationType eq 'Total'",
                "aggregationType eq 'Total'",
                "aggregationType eq 'Total'"
            };

            string[] filterArray =
            {
                $"({string.Join(" or ", nameArray)})",
                $"({string.Join(" or ", typeArray)})",
                $"startTime eq {startTime}",
                $"endTime eq {endTime}",
                "timeGrain eq duration'PT1M'"
            };

            return string.Join(" and ", filterArray);
        }

        private MetricsRequestsModel GetDefaultMetricsRequests()
        {
            var request = new MetricsRequestModel
            {
                HttpMethod = "GET",
                RelativeUrl = this.GetDefaultIoTHubMetricsUrl()
            };

            var result = new MetricsRequestsModel();
            result.Requests.Add(request);

            return result;
        }
    }
}
