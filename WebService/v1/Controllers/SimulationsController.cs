// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.PATH + "/[controller]"), ExceptionsFilter]
    public class SimulationsController : Controller
    {
        private const int MAX_DELETE_DEVICES = 100;

        private readonly ISimulations simulationsService;
        private readonly IServicesConfig servicesConfig;
        private readonly IDeploymentConfig deploymentConfig;
        private readonly IIotHubConnectionStringManager connectionStringManager;
        private readonly IPreprovisionedIotHub preprovisionedIotHub;
        private readonly ISimulationAgent simulationAgent;
        private readonly ISimulationRunner simulationRunner;
        private readonly IRateLimiting rateReporter;
        private readonly ILogger log;

        public SimulationsController(
            ISimulations simulationsService,
            IServicesConfig servicesConfig,
            IDeploymentConfig deploymentConfig,
            IIotHubConnectionStringManager connectionStringManager,
            IPreprovisionedIotHub preprovisionedIotHub,
            ISimulationAgent simulationAgent,
            ISimulationRunner simulationRunner,
            IRateLimiting rateReporter,
            ILogger logger)
        {
            this.simulationsService = simulationsService;
            this.servicesConfig = servicesConfig;
            this.deploymentConfig = deploymentConfig;
            this.connectionStringManager = connectionStringManager;
            this.preprovisionedIotHub = preprovisionedIotHub;
            this.simulationAgent = simulationAgent;
            this.simulationRunner = simulationRunner;
            this.rateReporter = rateReporter;
            this.log = logger;
        }

        [HttpGet]
        public async Task<SimulationListApiModel> GetAsync()
        {
            var simulationList = await this.simulationsService.GetListAsync();
            var simulationListApiModel = new SimulationListApiModel();
            foreach (var x in simulationList)
            {
                var simulationApiModel = SimulationApiModel.FromServiceModel(x);
                this.AppendHubPropertiesAndStatisticsAsync(simulationApiModel);
                simulationListApiModel.Items.Add(simulationApiModel);
            }

            return simulationListApiModel;
        }

        [HttpGet("{id}")]
        public async Task<SimulationApiModel> GetAsync(string id)
        {
            var simulation = await this.simulationsService.GetAsync(id);

            if (simulation == null)
            {
                this.log.Warn("Simulation not found", () => new { id });
                throw new BadRequestException("No data or invalid id provided.");
            }

            var simulationApiModel = SimulationApiModel.FromServiceModel(simulation);
            this.AppendHubPropertiesAndStatisticsAsync(simulationApiModel);

            return simulationApiModel;
        }

        [HttpPost]
        public async Task<SimulationApiModel> PostAsync(
            [FromBody] SimulationApiModel simulation,
            [FromQuery(Name = "template")] string template = "")
        {
            simulation?.ValidateInputRequest(this.log, this.connectionStringManager);

            if (simulation == null)
            {
                if (string.IsNullOrEmpty(template))
                {
                    this.log.Warn("No data or invalid data provided", () => new { simulation, template });
                    throw new BadRequestException("No data or invalid data provided.");
                }

                // Simulation can be created with `template=default` other than created with input data
                simulation = new SimulationApiModel();
            }

            return SimulationApiModel.FromServiceModel(
                await this.simulationsService.InsertAsync(simulation.ToServiceModel(), template));
        }

        [HttpPut("{id}")]
        public async Task<SimulationApiModel> PutAsync(
            [FromBody] SimulationApiModel simulation,
            string id = "")
        {
            simulation?.ValidateInputRequest(this.log, this.connectionStringManager);

            if (simulation == null)
            {
                this.log.Warn("No data provided, request object is null");
                throw new BadRequestException("No data provided, request object is empty.");
            }

            return SimulationApiModel.FromServiceModel(
                await this.simulationsService.UpsertAsync(simulation.ToServiceModel(id)));
        }

        [HttpPut("{id}/Devices!create")]
        public async Task PutAsync(
            [FromBody] CreateActionApiModel device)
        {
            if (device == null)
            {
                this.log.Warn("No data provided, request object is null");
                throw new BadRequestException("No data provided, request object is empty.");
            }

            device?.ValidateInputRequest(this.log);

            await this.simulationAgent.AddDeviceAsync(device.DeviceId, device.ModelId);
        }

        [HttpPut("{id}/Devices!batchDelete")]
        public async Task PutAsync(
            [FromBody] BatchDeleteActionApiModel devices)
        {
            if (devices == null)
            {
                this.log.Warn("No data provided, request object is null");
                throw new BadRequestException("No data provided, request object is empty.");
            }

            if (devices.DeviceIds.Count > MAX_DELETE_DEVICES)
            {
                this.log.Warn("Device count exceeded max allowed limit", () => new { MAX_DELETE_DEVICES });
                throw new BadRequestException("Device count exceeded max allowed limit (" + MAX_DELETE_DEVICES + ")");
            }

            await this.simulationAgent.DeleteDevicesAsync(devices.DeviceIds);
        }

        [HttpPatch("{id}")]
        public async Task<SimulationApiModel> PatchAsync(
            string id,
            [FromBody] SimulationPatchApiModel patch)
        {
            if (patch == null)
            {
                this.log.Warn("NULL patch provided", () => new { id });
                throw new BadRequestException("No data or invalid data provided");
            }

            var patchServiceModel = patch.ToServiceModel(id);

            if (patchServiceModel.Enabled == false)
            {
                patchServiceModel.Statistics = new Services.Models.SimulationStatistics
                {
                    AverageMessagesPerSecond = this.rateReporter.GetThroughputForMessages(),
                    TotalMessagesSent = this.simulationRunner.TotalMessagesCount
                };
            }

            return SimulationApiModel.FromServiceModel(
                await this.simulationsService.MergeAsync(patchServiceModel));
        }

        [HttpDelete("{id}")]
        public async Task DeleteAsync(string id)
        {
            await this.simulationsService.DeleteAsync(id);
        }

        // Append additional Hub properties and Statistics 
        private void AppendHubPropertiesAndStatisticsAsync(SimulationApiModel simulationApiModel)
        {
            var isRunning = simulationApiModel.Running == true;

            foreach (var iotHub in simulationApiModel.IotHubs)
            {
                // Preprovisioned hub status
                var isHubPreprovisioned = this.IsHubConnectionStringConfigured();
                iotHub.PreprovisionedIoTHub = isHubPreprovisioned;

                if (isHubPreprovisioned && isRunning)
                {
                    iotHub.PreprovisionedIoTHubInUse = this.IsPreprovisionedIoTHubInUse();
                    var preprovisionedIoTHubMetricsUrl = this.GetIoTHubMetricsUrl();
                    iotHub.PreprovisionedIoTHubMetricsUrl = preprovisionedIoTHubMetricsUrl;
                }
            }

            if (isRunning)
            {
                // Average messages per second count
                simulationApiModel.Statistics.AverageMessagesPerSecond = this.rateReporter.GetThroughputForMessages();

                // Total messages count
                simulationApiModel.Statistics.TotalMessagesSent = this.simulationRunner.TotalMessagesCount;

                // Active devices count
                simulationApiModel.Statistics.ActiveDevicesCount = this.simulationRunner.ActiveDevicesCount;

                // Failed telemetry messages count
                simulationApiModel.Statistics.FailedMessagesCount = this.simulationRunner.FailedMessagesCount;

                // Failed device connections count
                simulationApiModel.Statistics.FailedDeviceConnectionsCount = this.simulationRunner.FailedDeviceConnectionsCount;

                // Failed device connections count
                simulationApiModel.Statistics.FailedDeviceTwinUpdatesCount = this.simulationRunner.FailedDeviceTwinUpdatesCount;

                // Simulation errors count
                simulationApiModel.Statistics.SimulationErrorsCount = this.simulationRunner.SimulationErrorsCount;
            }
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

        // Check whether the simulation is running with the conn string in the configuration
        private bool IsPreprovisionedIoTHubInUse()
        {
            var csInUse = this.connectionStringManager.GetIotHubConnectionString().ToLowerInvariant().Trim();
            var csInConf = this.servicesConfig?.IoTHubConnString?.ToLowerInvariant().Trim();

            return csInUse == csInConf;
        }

        // If the simulation is running with the conn string in the config then return a URL to the metrics
        private string GetIoTHubMetricsUrl()
        {
            var csInUse = this.connectionStringManager.GetIotHubConnectionString().ToLowerInvariant().Trim();
            var csInConf = this.servicesConfig?.IoTHubConnString?.ToLowerInvariant().Trim();

            // Return the URL only when the simulation is running with the configured conn string
            if (csInUse != csInConf) return string.Empty;

            return $"https://portal.azure.com/{this.deploymentConfig.AzureSubscriptionDomain}" +
                   $"#resource/subscriptions/{this.deploymentConfig.AzureSubscriptionId}" +
                   $"/resourceGroups/{this.deploymentConfig.AzureResourceGroup}" +
                   $"/providers/Microsoft.Devices/IotHubs/{this.deploymentConfig.AzureIothubName}/Metrics";
        }
    }
}
