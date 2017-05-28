// Copyright (c) Microsoft. All rights reserved.

using System.Web.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;
using Microsoft.Web.Http;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [ApiVersion(Version.Number)]
    public class SimulationsController : ApiController
    {
        private readonly ISimulations simulationsService;

        public SimulationsController(ISimulations simulationsService)
        {
            this.simulationsService = simulationsService;
        }

        public SimulationListApiModel Get()
        {
            return new SimulationListApiModel(this.simulationsService.GetList());
        }

        public SimulationApiModel Get(string id)
        {
            return new SimulationApiModel(this.simulationsService.Get(id));
        }

        public SimulationApiModel Post(SimulationApiModel simulation)
        {
            if (simulation == null) throw new BadRequestException("No data or invalid data provided.");

            return new SimulationApiModel(
                this.simulationsService.Insert(simulation.ToServiceModel("")));
        }

        public SimulationApiModel Put(string id, SimulationApiModel simulation)
        {
            if (simulation == null) throw new BadRequestException("No data or invalid data provided.");

            return new SimulationApiModel(
                this.simulationsService.Upsert(simulation.ToServiceModel(id)));
        }

        public SimulationApiModel Patch(string id, SimulationPatchApiModel patch)
        {
            if (patch == null) throw new BadRequestException("No data or invalid data provided");

            return new SimulationApiModel(
                this.simulationsService.Merge(patch.ToServiceModel(id)));
        }
    }
}
