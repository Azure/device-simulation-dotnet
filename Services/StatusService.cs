// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public class StatusService : IStatusService
    {
        private const string JSON_TRUE = "true";
        private const string JSON_FALSE = "false";

        private const string SIMULATION_RUNNING_KEY = "SimulationRunning";
        private const string PREPROVISIONED_IOTHUB_KEY = "PreprovisionedIoTHub";

        private const bool ALLOW_INSECURE_SSL_SERVER = true;
        private readonly int timeoutMS = 10000;

        private readonly IPreprovisionedIotHub preprovisionedIotHub;
        private readonly ISimulations simulations;
        private readonly IHttpClient httpClient;
        private readonly ILogger log;
        private readonly IServicesConfig servicesConfig;

        public StatusService(
            ILogger logger,
            IPreprovisionedIotHub preprovisionedIotHub,
            ISimulations simulations,
            IHttpClient httpClient,
            IServicesConfig servicesConfig)
        {
            this.log = logger;
            this.preprovisionedIotHub = preprovisionedIotHub;
            this.simulations = simulations;
            this.httpClient = httpClient;
            this.servicesConfig = servicesConfig;
        }

        public async Task<StatusServiceModel> GetStatusAsync()
        {
            var result = new StatusServiceModel(true, "Alive and well!");
            var errors = new List<string>();

            string storageAdapterName = "StorageAdapter";
            string diagnosticsName = "Diagnostics";

            // Simulation status
            var simulationIsRunning = await this.CheckIsSimulationRunningAsync(errors);
            var isRunning = simulationIsRunning.HasValue && simulationIsRunning.Value;

            // Check access to StorageAdapter
            var storageAdapterResult = await this.PingServiceAsync(
                storageAdapterName,
                this.servicesConfig.StorageAdapterApiUrl);
            this.SetServiceStatus(storageAdapterName, storageAdapterResult, result, errors);

            // Check access to Diagnostics
            var diagnosticsResult = await this.PingServiceAsync(
                diagnosticsName,
                this.servicesConfig.DiagnosticsEndpointUrl);
            // Note: Overall simulation service status is independent of diagnostics service
            // Hence not using SetServiceStatus on diagnosticsResult
            result.Dependencies.Add(diagnosticsName, diagnosticsResult);

            // Preprovisioned IoT hub status
            var isHubPreprovisioned = this.IsHubConnectionStringConfigured();

            if (isHubPreprovisioned)
            {
                var preprovisionedHubResult = await this.preprovisionedIotHub.PingRegistryAsync();
                this.SetServiceStatus("IoTHub", preprovisionedHubResult, result, errors);
            }

            result.Properties.Add(SIMULATION_RUNNING_KEY,
                simulationIsRunning.HasValue
                    ? (isRunning ? JSON_TRUE : JSON_FALSE)
                    : "unknown");
            result.Properties.Add(PREPROVISIONED_IOTHUB_KEY,
                isHubPreprovisioned
                    ? JSON_TRUE
                    : JSON_FALSE);

            if (errors.Count > 0)
            {
                result.Status.Message = string.Join("; ", errors);
            }

            result.Properties.Add("DiagnosticsEndpointUrl", this.servicesConfig?.DiagnosticsEndpointUrl);
            result.Properties.Add("StorageAdapterApiUrl", this.servicesConfig?.StorageAdapterApiUrl);
            this.log.Info(
                "Service status request",
                () => new
                {
                    Healthy = result.Status.IsHealthy,
                    result.Status.Message
                });
            return result;
        }

        private void SetServiceStatus(
            string dependencyName,
            StatusResultServiceModel serviceResult,
            StatusServiceModel result,
            List<string> errors)
        {
            if (!serviceResult.IsHealthy)
            {
                errors.Add(dependencyName + " check failed");
                result.Status.IsHealthy = false;
            }

            result.Dependencies.Add(dependencyName, serviceResult);
        }

        // Check whether the simulation is running, and populate errors if any
        private async Task<bool?> CheckIsSimulationRunningAsync(List<string> errors)
        {
            bool? simulationRunning = null;
            try
            {
                var simulationList = await this.simulations.GetListAsync();
                var runningSimulation = simulationList.FirstOrDefault(s => s.ShouldBeRunning);
                simulationRunning = (runningSimulation != null);
            }
            catch (Exception e)
            {
                var msg = "Unable to fetch simulation status";
                errors.Add(msg);
                this.log.Error(msg, e);
            }

            return simulationRunning;
        }

        // Check whether the configuration contains a connection string
        private bool IsHubConnectionStringConfigured()
        {
            var cs = this.servicesConfig?.IoTHubConnString?.ToLowerInvariant().Trim();
            return (!string.IsNullOrEmpty(cs)
                    && cs.Contains("hostname=")
                    && cs.Contains("sharedaccesskeyname=")
                    && cs.Contains("sharedaccesskey="));
        }

        private async Task<StatusResultServiceModel> PingServiceAsync(string serviceName, string serviceUrl)
        {
            var result = new StatusResultServiceModel(false, $"{serviceName} check failed");
            try
            {
                var response = await this.httpClient.GetAsync(this.PrepareRequest($"{serviceUrl}/status"));
                if (response.IsError)
                {
                    result.Message = $"Status code: {response.StatusCode}; Response: {response.Content}";
                }
                else
                {
                    var data = JsonConvert.DeserializeObject<StatusServiceModel>(response.Content);
                    result = data.Status;
                }
            }
            catch (Exception e)
            {
                this.log.Error(result.Message, () => new { e });
            }

            return result;
        }

        private HttpRequest PrepareRequest(string path)
        {
            var request = new HttpRequest();
            request.AddHeader(HttpRequestHeader.Accept.ToString(), "application/json");
            request.AddHeader(HttpRequestHeader.CacheControl.ToString(), "no-cache");
            request.AddHeader(HttpRequestHeader.Referer.ToString(), "ASA Manager " + this.GetType().FullName);
            request.SetUriFromString(path);
            request.Options.EnsureSuccess = false;
            request.Options.Timeout = this.timeoutMS;
            if (path.ToLowerInvariant().StartsWith("https:"))
            {
                request.Options.AllowInsecureSSLServer = ALLOW_INSECURE_SSL_SERVER;
            }

            this.log.Debug("Prepare Request", () => new { request });

            return request;
        }
    }
}
