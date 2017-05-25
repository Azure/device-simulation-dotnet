// Copyright (c) Microsoft. All rights reserved.

using System.Web.Http;
using Microsoft.Web.Http;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    /// <summary>
    /// The simulation resource class. The controller follows ASP.NET naming
    /// convention, however the service allows to create only 1 simulation, with
    /// ID "1".
    /// </summary>
    [ApiVersion(Version.Number)]
    public class SimulationsController : ApiController
    {
        /// <summary>
        /// Retrieve the simulation status, e.g. whether the simulation is
        /// running. The response include some information about the device
        /// types and the number of devices being simulated.
        /// </summary>
        /// <param name="id">Always "1", there is only one simulation</param>
        public string Get(string id)
        {
            return "TODO";
        }

        /// <summary>
        /// Create or modify the simulation, for instance change the device
        /// types and the count of devices being simulated.
        /// When modifying the existing simulation, the service uses optimistic
        /// concurrency to validate the request.
        /// </summary>
        /// <param name="id">Always "1", there is only one simulation</param>
        public string Put(string id)
        {
            return "TODO";
        }

        /// <summary>
        /// Modify part of the simulation, for instance start and stop the
        /// simulation
        /// </summary>
        /// <param name="id">Always "1", there is only one simulation</param>
        public string Patch(string id)
        {
            return "TODO";
        }
    }
}
