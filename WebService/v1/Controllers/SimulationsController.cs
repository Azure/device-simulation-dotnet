// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.PATH + "/[controller]"), ExceptionsFilter]
    public class SimulationsController : Controller
    {
        private const int MAX_DELETE_DEVICES = 100;
        private readonly ISimulations simulationsService;
        private readonly IIotHubConnectionStringManager connectionStringManager;
        private readonly ISimulationAgent simulationAgent;

        private readonly ILogger log;

        public SimulationsController(
            ISimulations simulationsService,
            IIotHubConnectionStringManager connectionStringManager,
            ISimulationAgent simulationAgent,
            ILogger logger)
        {
            this.simulationsService = simulationsService;
            this.connectionStringManager = connectionStringManager;
            this.simulationAgent = simulationAgent;
            this.log = logger;
        }

        [HttpGet]
        public async Task<SimulationListApiModel> GetAsync()
        {
            return new SimulationListApiModel(await this.simulationsService.GetListAsync());
        }

        [HttpGet("{id}")]
        public async Task<SimulationApiModel> GetAsync(string id)
        {
            return SimulationApiModel.FromServiceModel(await this.simulationsService.GetAsync(id));
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
                this.log.Warn("No data or invalid data provided", () => new { simulation });
                throw new BadRequestException("No data or invalid data provided.");
            }

            return SimulationApiModel.FromServiceModel(
                await this.simulationsService.UpsertAsync(simulation.ToServiceModel(id)));
        }

        [HttpPut("{id}/AddDevice")]
        public async Task PutAsync(
        [FromBody] DeviceApiModel device)
        {
            if (device == null)
            {
                this.log.Warn("Invalid input data provided", () => new { device });
                throw new BadRequestException("Invalid input data provided.");
            }

            await this.simulationAgent.AddDevice(device.Item);
        }

        [HttpPut("{id}/DeleteDevices")]
        public async Task PutAsync(
            [FromBody] DeviceListApiModel devices)
        {
            if (devices == null)
            {
                this.log.Warn("Invalid input data provided", () => new { devices });
                throw new BadRequestException("Invalid input data provided.");
            }

            if (devices.Items.Count > MAX_DELETE_DEVICES)
            {
                this.log.Warn("Device count exceeded max allowed limit", () => new { MAX_DELETE_DEVICES });
                throw new BadRequestException("Device count exceeded max allowed limit");
            }

            await this.simulationAgent.DeleteDevices(devices.Items);
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

            return SimulationApiModel.FromServiceModel(
                await this.simulationsService.MergeAsync(patch.ToServiceModel(id)));
        }

        [HttpDelete("{id}")]
        public async Task DeleteAsync(string id)
        {
            await this.simulationsService.DeleteAsync(id);
        }
    }
}
