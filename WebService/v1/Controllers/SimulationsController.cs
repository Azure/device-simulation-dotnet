// Copyright (c) Microsoft. All rights reserved.

using System.Web.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
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

        public SimulationApiModel Put(SimulationApiModel simulation)
        {
            return new SimulationApiModel(
                this.simulationsService.Create(simulation.ToServiceModel()));
        }

        public SimulationApiModel Patch(SimulationPatchApiModel patch)
        {
            return new SimulationApiModel(
                this.simulationsService.Merge(patch.ToServiceModel()));
        }
    }
}
