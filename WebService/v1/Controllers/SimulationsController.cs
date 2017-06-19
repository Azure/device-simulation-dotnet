// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [Route(Version.Path + "/[controller]"), ExceptionsFilter]
    public class SimulationsController : Controller
    {
        private readonly ISimulations simulationsService;

        public SimulationsController(ISimulations simulationsService)
        {
            this.simulationsService = simulationsService;
        }

        [HttpGet]
        public SimulationListApiModel Get()
        {
            return new SimulationListApiModel(this.simulationsService.GetList());
        }

        [HttpGet("{id}")]
        public SimulationApiModel Get(string id)
        {
            return new SimulationApiModel(this.simulationsService.Get(id));
        }

        [HttpPost]
        public SimulationApiModel Post(
            [FromBody] SimulationApiModel simulation,
            [FromQuery(Name = "template")] string template = "")
        {
            if (simulation == null)
            {
                if (string.IsNullOrEmpty(template))
                    throw new BadRequestException("No data or invalid data provided.");

                simulation = new SimulationApiModel();
            }

            return new SimulationApiModel(
                this.simulationsService.Insert(simulation.ToServiceModel(), template));
        }

        [HttpPut("{id}")]
        public SimulationApiModel Put(
            [FromBody] SimulationApiModel simulation,
            string id = "",
            [FromQuery(Name = "template")] string template = "")
        {
            if (simulation == null)
            {
                if (string.IsNullOrEmpty(template))
                    throw new BadRequestException("No data or invalid data provided.");

                simulation = new SimulationApiModel();
            }

            return new SimulationApiModel(
                this.simulationsService.Upsert(simulation.ToServiceModel(id), template));
        }

        [HttpPatch("{id}")]
        public SimulationApiModel Patch(
            string id, 
            [FromBody] SimulationPatchApiModel patch)
        {
            if (patch == null) throw new BadRequestException("No data or invalid data provided");

            return new SimulationApiModel(
                this.simulationsService.Merge(patch.ToServiceModel(id)));
        }
    }
}
