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
                    var newSimulation = (await this.simulations.GetListAsync()).FirstOrDefault();

                    // if the simulation is removed from storage & we're running stop simulation.
                    if (newSimulation == null && this.simulation != null)
                    {
                        this.runner.Stop();
                        this.simulation = null;
                        this.log.Debug("---------------------------------", () => { });
                        this.log.Info("No simulation found in storage...", () => { });
                        this.log.Debug("---------------------------------", () => { });
                    }

                    // if there's a new simulation and it's different from the current one 
                    // stop the current one from running & restart w/ the new one if enabled
                    if (newSimulation != null && this.simulation != null
                        && this.IsSimulationModifiedOrDisabled(newSimulation))
                    {
                        this.log.Debug("New simulation found, stopping and restarting", () => { });

                        this.runner.Stop();

                        this.simulation = newSimulation;
                        if (this.simulation.Enabled)
                        { 
                            this.runner.Start(this.simulation);
                            this.log.Debug("----Started new simulation found in storage------", () => this.simulation);
                        }
                    }

                    // if there's no simulation running but there's one from storage start it 
                    if (newSimulation != null && this.simulation == null)
                    {
                        this.simulation = newSimulation;
                        if (this.simulation.Enabled)
                            this.runner.Start(this.simulation);
                    }

                    // if the current simulation was asked to stop, stop it.
                    if (this.simulation != null && this.simulation.Enabled == false)
                    {
                        this.runner.Stop();
                    }

                }
                catch (Exception e)
                {
                    this.log.Error("Failure reading and starting simulation from storage.", () => new { e });
                }

                // TODO: Decide whether to remove this code - I found it useful for debugging
                if (this.simulation != null && this.simulation.Enabled == true)
                {
                    this.log.Debug("---------------------------------", () => { });
                    this.log.Debug("----Current simulation being run------", () => { });
                    foreach (var model in this.simulation.DeviceModels)
                    {
                        this.log.Debug("Device:", () => model );
                    }
                    this.log.Debug("---------------------------------", () => { });
                    this.log.Debug("---------------------------------", () => { });
                }

                Thread.Sleep(CHECK_INTERVAL);
            }
        }

        private bool IsSimulationModifiedOrDisabled(Services.Models.Simulation newSimulation)
        {
            return newSimulation.Modified != this.simulation.Modified 
                || this.simulation.Enabled == false;
        }
    }
}
