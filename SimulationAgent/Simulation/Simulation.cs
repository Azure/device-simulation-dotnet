// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation
{
    public interface ISimulation
    {
        Task RunAsync();
    }

    public class Simulation : ISimulation
    {
        private const int CHECK_INTERVAL = 3000;

        private readonly ILogger log;
        private readonly ISimulations simulations;
        private readonly ISimulationRunner runner;

        public Simulation(
            ILogger logger,
            ISimulations simulations,
            ISimulationRunner runner)
        {
            this.log = logger;
            this.simulations = simulations;
            this.runner = runner;
        }

        public async Task RunAsync()
        {
            this.log.Info("Simulation Agent running", () => { });

            // Keep running, checking if the simulation state changes
            while (true)
            {
                Services.Models.Simulation simulation = null;

                try
                {
                    simulation = (await this.simulations.GetListAsync()).FirstOrDefault();
                }
                catch (Exception e)
                {
                    this.log.Error("Unable to load simulation", () => new { e });
                }

                if (simulation != null && simulation.Enabled)
                {
                    this.runner.Start(simulation);
                }
                else
                {
                    this.runner.Stop();
                }

                Thread.Sleep(CHECK_INTERVAL);
            }
        }
    }
}
