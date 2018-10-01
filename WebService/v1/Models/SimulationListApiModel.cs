// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class SimulationListApiModel
    {
        [JsonProperty(PropertyName = "Items")]
        public List<SimulationApiModel.SimulationApiModel> Items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "SimulationList;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/simulations" }
        };

        public SimulationListApiModel()
        {
            this.Items = new List<SimulationApiModel.SimulationApiModel>();
        }

        /// <summary>Map a service model to the corresponding API model</summary>	
        public SimulationListApiModel(
            IEnumerable<Services.Models.Simulation> simulations,
            IServicesConfig servicesConfig,
            IDeploymentConfig deploymentConfig,
            IIotHubConnectionStringManager connectionStringManager,
            ISimulationRunner simulationRunner,
            IRateLimiting rateReporter)
        {
            this.Items = new List<SimulationApiModel.SimulationApiModel>();
            foreach (var x in simulations)
            {
                this.Items.Add(
                    SimulationApiModel.SimulationApiModel.FromServiceModelAsync(
                        x,
                        servicesConfig,
                        deploymentConfig,
                        connectionStringManager,
                        simulationRunner,
                        rateReporter).Result);
            }
        }
    }
}
