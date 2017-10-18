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
        private const int CHECK_INTERVAL_MSECS = 10000;

        private readonly ILogger log;
        private readonly ISimulations simulations;
        private readonly ISimulationRunner runner;
        private Services.Models.Simulation simulation;

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

            // Keep running, checking if the simulation changes
            while (true)
            {
                try
                {
                    this.log.Debug("----Checking for simulation changes------", () => { });

                    var newSimulation = (await this.simulations.GetListAsync()).FirstOrDefault();

                    // if the simulation is removed from storage & we're running stop simulation.
                    this.CheckForDeletedSimulation(newSimulation);

                    // if there's a new simulation and it's different from the current one
                    // stop the current one from running & start the new one if it's enabled
                    this.CheckForChangedSimulation(newSimulation);

                    // if there's no simulation running but there's one from storage start it
                    this.CheckForNewSimulation(newSimulation);

                    // if the current simulation was asked to stop, stop it.
                    this.CheckForStopOrStartToSimulation();
                }
                catch (Exception e)
                {
                    this.log.Error("Failure reading and starting simulation from storage.", () => new { e });
                }

                if (this.simulation != null && this.simulation.Enabled == true)
                {
                    this.log.Debug("----Current simulation being run------", () => { });
                    foreach (var model in this.simulation.DeviceModels)
                    {
                        this.log.Debug("Device model", () => new { model });
                    }
                }

                Thread.Sleep(CHECK_INTERVAL_MSECS);
            }
        }

        private void CheckForDeletedSimulation(Services.Models.Simulation newSimulation)
        {
            if (newSimulation == null && this.simulation != null)
            {
                this.runner.Stop();
                this.simulation = null;
                this.log.Info("No simulation found in storage...", () => { });
            }
        }

        private void CheckForChangedSimulation(Services.Models.Simulation newSimulation)
        {
            if (newSimulation != null && this.simulation != null &&
                newSimulation.Modified != this.simulation.Modified)
            {
                this.log.Debug("The simulation has been modified, stopping the current " +
                               "simulation and starting the new one if enabled", () => { });
                this.runner.Stop();

                this.simulation = newSimulation;
                if (this.simulation.Enabled)
                {
                    this.runner.Start(this.simulation);
                    this.log.Debug("----Started new simulation ------", () => new { this.simulation });
                }
            }
        }

        private void CheckForNewSimulation(Services.Models.Simulation newSimulation)
        {
            if (newSimulation != null && this.simulation == null)
            {
                this.simulation = newSimulation;
                if (this.simulation.Enabled)
                    this.runner.Start(this.simulation);
            }
        }

        private void CheckForStopOrStartToSimulation()
        {
            // stopped
            if (this.simulation != null && this.simulation.Enabled == false)
            {
                this.runner.Stop();
            }

            // started
            if (this.simulation != null && this.simulation.Enabled == true)
            {
                this.runner.Start(this.simulation);
            }
        }
    }
}
