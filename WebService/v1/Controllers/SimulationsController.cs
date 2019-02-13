// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
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

        private readonly IConnectionStringValidation connectionStringValidation;
        private readonly IIothubMetrics iothubMetrics;
        private readonly IRateLimitingConfig defaultRatingConfig;
        private readonly ISimulationAgent simulationAgent;
        private readonly IFactory factory;

        private readonly ILogger log;

        public SimulationsController(
            ISimulations simulationsService,
            IConnectionStringValidation connectionStringValidation,
            IIothubMetrics iothubMetrics,
            IRateLimitingConfig defaultRatingConfig,
            IPreprovisionedIotHub preprovisionedIotHub,
            ISimulationAgent simulationAgent,
            IFactory factory,
            ILogger logger)
        {
            this.simulationsService = simulationsService;
            this.connectionStringValidation = connectionStringValidation;
            this.iothubMetrics = iothubMetrics;
            this.defaultRatingConfig = defaultRatingConfig;
            this.simulationAgent = simulationAgent;
            this.factory = factory;
            this.log = logger;
        }

        [HttpGet]
        public async Task<SimulationListApiModel> GetAsync()
        {
            var simulationList = await this.simulationsService.GetListWithStatisticsAsync();
            return new SimulationListApiModel(simulationList);
        }

        [HttpGet("{id}")]
        public async Task<SimulationApiModel> GetAsync(string id)
        {
            var simulation = await this.simulationsService.GetWithStatisticsAsync(id);
            return SimulationApiModel.FromServiceModel(simulation);
        }

        [HttpPost]
        public async Task<SimulationApiModel> PostAsync(
            [FromBody]
            SimulationApiModel simulationApiModel,
            [FromQuery(Name = "template")]
            string template = "")
        {
            if (simulationApiModel != null)
            {
                await simulationApiModel.ValidateInputRequestAsync(this.log, this.connectionStringValidation);
            }
            else
            {
                if (string.IsNullOrEmpty(template))
                {
                    this.log.Warn("No data or invalid data provided", () => new { simulationApiModel, template });
                    throw new BadRequestException("No data or invalid data provided.");
                }

                // Simulation can be created with `template=default` other than created with input data
                simulationApiModel = new SimulationApiModel();
            }

            var simulation = await this.simulationsService.InsertAsync(simulationApiModel.ToServiceModel(null, this.defaultRatingConfig), template);
            return SimulationApiModel.FromServiceModel(simulation);
        }

        [HttpPost("{id}/metrics/iothub!search")]
        public async Task<object> PostAsync(string id, [FromBody]
            MetricsRequestsApiModel requests)
        {
            // API payload validation is not required as we're simply relaying the request.
            var payload = requests?.ToServiceModel();

            // Service will generate default query if payload is null.
            // See default query details in /Services/AzureManagementAdapter/AzureManagementAdapter.cs
            return await this.iothubMetrics.GetIothubMetricsAsync(payload);
        }

        [HttpPut("{id}")]
        public async Task<SimulationApiModel> PutAsync([FromBody]
            SimulationApiModel simulationApiModel, string id = "")
        {
            if (simulationApiModel == null)
            {
                this.log.Warn("No data provided, request object is null");
                throw new BadRequestException("No data provided, request object is empty.");
            }

            await simulationApiModel.ValidateInputRequestAsync(this.log, this.connectionStringValidation);

            // Load the existing resource, so that internal properties can be copied
            var existingSimulation = await this.GetExistingSimulationAsync(id);

            var simulation = await this.simulationsService.UpsertAsync(
                simulationApiModel.ToServiceModel(existingSimulation, this.defaultRatingConfig, id),
                true);

            return SimulationApiModel.FromServiceModel(simulation);
        }

        [HttpPut("{id}/devices!create")]
        public async Task PutAsync([FromBody]
            CreateActionApiModel device, string id = "")
        {
            if (device == null)
            {
                this.log.Warn("No data provided, request object is null");
                throw new BadRequestException("No data provided, request object is empty.");
            }

            device.ValidateInputRequest(this.log);

            await this.simulationAgent.AddDeviceAsync(id, device.DeviceId, device.ModelId);
        }

        [HttpPut("{id}/devices!batchDelete")]
        public async Task PutAsync([FromBody]
            BatchDeleteActionApiModel devices)
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

        // TODO: use the connection string of the simulation, instead of passing the value.
        [HttpPut("{id}/devices!deleteAll")]
        public async Task<object> PutEraseHubAsync(string id, [FromBody]
            IoTHubApiModel hubDetails)
        {
            if (hubDetails == null) throw new BadRequestException("Hub details are missing");

            var devices = this.factory.Resolve<IDevices>();
            devices.TmpInit(hubDetails.ConnectionString);
            var result = await this.simulationsService.DeleteAllDevicesAsync(id, devices);
            devices.Dispose();

            return result;
        }

        // TODO: save statistics to storage during patch
        [HttpPatch("{id}")]
        public async Task<SimulationApiModel> PatchAsync(string id, [FromBody]
            SimulationPatchApiModel patch)
        {
            if (patch == null)
            {
                this.log.Warn("NULL patch provided", () => new { id });
                throw new BadRequestException("No data or invalid data provided");
            }

            SimulationPatch patchServiceModel = patch.ToServiceModel(id);
            var simulation = await this.simulationsService.MergeAsync(patchServiceModel);
            return SimulationApiModel.FromServiceModel(simulation);
        }

        [HttpDelete("{id}")]
        public async Task DeleteAsync(string id)
        {
            await this.simulationsService.DeleteAsync(id);
        }

        private async Task<Simulation> GetExistingSimulationAsync(string id)
        {
            try
            {
                return await this.simulationsService.GetAsync(id);
            }
            catch (ResourceNotFoundException)
            {
                return null;
            }
        }
    }
}
