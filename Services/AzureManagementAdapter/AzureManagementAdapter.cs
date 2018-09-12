// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net;
using System.Net.Http;
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
        Task<MetricsResponseModel> PostAsync(string token, MetricsRequestsModel requests);
    }

    public class AzureManagementAdapterClient : IAzureManagementAdapterClient
    {
        private const string DATE_FORMAT = "yyyy-MM-ddTHH:mm:ssZ";
        private const string METIRCS_API_VERSION = "2016-06-01";
        private const bool ALLOW_INSECURE_SSL_SERVER = true;
        private readonly IHttpClient httpClient;
        private readonly ILogger log;
        private readonly IServicesConfig config;
        private readonly IDeploymentConfig deploymentConfig;

        public AzureManagementAdapterClient(
            IHttpClient httpClient,
            IServicesConfig config,
            IDeploymentConfig deploymentConfig,
            ILogger logger)
        {
            this.httpClient = httpClient;
            this.log = logger;
            this.config = config;
            this.deploymentConfig = deploymentConfig;
        }

        public async Task<MetricsResponseModel> PostAsync(string token, MetricsRequestsModel requests)
        {
            if (requests == null)
            {
                requests = this.GetMetricsRequests();
            }

            var request = this.PrepareRequest($"batch?api-version={this.config.AzureManagementAdapterApiVersion}", token, requests);
            var response = await this.httpClient.PostAsync(request);

            this.log.Debug("Azure Management response", () => new { response });

            this.ThrowIfError(response);

            return JsonConvert.DeserializeObject<MetricsResponseModel>(response.Content);
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
            if (this.config.AzureManagementAdapterApiUrl.ToLowerInvariant().StartsWith("https:"))
            {
                request.Options.AllowInsecureSSLServer = ALLOW_INSECURE_SSL_SERVER;
            }

            if (content != null)
            {
                request.SetContent(content);
            }

            this.log.Debug("Azure Management request", () => new { request });

            return request;
        }

        private void ThrowIfError(IHttpResponse response)
        {
            if (response.IsError)
            {
                throw new ExternalDependencyException(
                    new HttpRequestException($"Metrics request error: status code {response.StatusCode}"));
            }
        }

        private string GetIoTHubMetricsUrl()
        {
            return $"/subscriptions/{this.deploymentConfig.AzureSubscriptionId}" +
                   $"/resourceGroups/{this.deploymentConfig.AzureResourceGroup}" +
                   $"/providers/Microsoft.Devices/IotHubs/{this.deploymentConfig.AzureIothubName}" +
                   $"/providers/Microsoft.Insights/metrics?api-version={METIRCS_API_VERSION}&" +
                   $"$filter={this.GetMetricsQuery()}";
        }

        private string GetMetricsQuery()
        {
            // TODO: consider using query params from controller
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

            string[] filterArray = {
                $"({string.Join(" or ", nameArray)})",
                $"({string.Join(" or ", typeArray)})",
                $"startTime eq {startTime}",
                $"endTime eq {endTime}",
                "timeGrain eq duration'PT1M'"
            };
            
            return string.Join(" and ", filterArray);
        }

        private MetricsRequestsModel GetMetricsRequests()
        {
            MetricsRequestModel request = new MetricsRequestModel
            {
                HttpMethod = "GET",
                RelativeUrl = this.GetIoTHubMetricsUrl()
            };

            MetricsRequestsModel result = new MetricsRequestsModel();
            result.Requests.Add(request);

            return result;
        }
    }
}
