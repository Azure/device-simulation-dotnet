// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation
{
    public interface ISimulation
    {
        void Run();
    }

    public class Simulation : ISimulation
    {
        private const int CheckInterval = 3000;

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

        public void Run()
        {
            this.log.Info("Simulation Agent running", () => { });

            // Keep running, checking if the simulation state changes
            while (true)
            {
                var simulation = this.simulations.GetList().FirstOrDefault();
                if (simulation != null && simulation.Enabled)
                {
                    this.runner.Start(simulation);
                }
                else
                {
                    this.runner.Stop();
                }

                Thread.Sleep(CheckInterval);
            }
        }
    }
}
