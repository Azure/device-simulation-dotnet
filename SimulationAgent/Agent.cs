// Copyright (c) Microsoft. All rights reserved. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    public interface ISimulationAgent
    {
        Task RunAsync();
        Task AddDevice(string id);
        Task DeleteDevices(List<string> ids);
        void Stop();
    }

    public class Agent : ISimulationAgent
    {
        private const int CHECK_INTERVAL_MSECS = 10000;

        private readonly ILogger log;
        private readonly ISimulations simulations;
        private readonly ISimulationRunner runner;
        private Services.Models.Simulation simulation;
        private bool running;

        public Agent(
            ILogger logger,
            ISimulations simulations,
            ISimulationRunner runner)
        {
            this.log = logger;
            this.simulations = simulations;
            this.runner = runner;
            this.running = true;
        }

        public async Task RunAsync()
        {
            this.log.Info("Simulation Agent running", () => { });

            // Keep running, checking if the simulation changes
            while (this.running)
            {
                var oldSimulation = this.simulation;
                try
                {
                    this.log.Debug("------ Checking for simulation changes ------", () => { });

                    var newSimulation = (await this.simulations.GetListAsync()).FirstOrDefault();
                    this.log.Debug("Simulation loaded", () => new { newSimulation });

                    // if the simulation is removed from storage & we're running stop simulation.
                    this.CheckForDeletedSimulation(newSimulation);

                    // if there's no simulation running but there's one from storage start it
                    this.CheckForNewSimulation(newSimulation);

                    // if the current simulation was asked to stop, stop it.
                    this.CheckForStopOrStartToSimulation();
                }
                catch (Exception e)
                {
                    this.log.Error("Failure reading and starting simulation from storage.", () => new { e });
                    this.simulation = oldSimulation;
                }

                if (this.simulation != null && this.simulation.ShouldBeRunning())
                {
                    this.log.Debug("------ Current simulation being run ------", () => { });
                    foreach (var model in this.simulation.DeviceModels)
                    {
                        this.log.Debug("Device model", () => new { model });
                    }
                }

                Thread.Sleep(CHECK_INTERVAL_MSECS);
            }
        }

        public void Stop()
        {
            this.running = false;
            this.runner.Stop();
        }

        public async Task AddDevice(string id)
        {
            // currently only one simulation is supported
            var simulation = (await this.simulations.GetListAsync()).FirstOrDefault();
            var modelIds = this.parseModelIds(new List<string>() { id });
            DeviceModelRef modelToUpdate = null;

            if (simulation != null)
            {
                foreach (var model in simulation.DeviceModels)
                {
                    if (modelIds.Contains(model.Id))
                    {
                        modelToUpdate = model;
                        model.Count++;
                    }
                }
            }

            if (modelToUpdate != null) 
            {
                try
                {
                    if (this.running)
                    {
                        this.log.Info("Add device to running simulation", () => { });
                        await this.runner.AddDevice(modelToUpdate, this.parsePosition(id));
                    }
                    else
                    {
                        this.log.Info("Add device to hub", () => { });
                        await this.simulations.AddDeviceAsync(id);
                    }
                }
                catch (Exception e)
                {
                    this.log.Debug("Error while creating new device", () => new { e });
                    throw new Exception("Error while creating a new device");
                }

                await this.simulations.UpsertAsync(simulation);
            }
        }


        /// <summary>
        /// Delete a list of devices
        /// </summary>
        public async Task DeleteDevices(List<string> ids)
        {
            this.log.Info("Update simulation", () => { });

            // currently only one simulation is supported
            var simulation = (await this.simulations.GetListAsync()).FirstOrDefault();
            var modelIds = this.parseModelIds(ids);
            bool shouldUpdateSimulation = false;

            if (simulation != null)
            {
                foreach (var model in simulation.DeviceModels)
                {
                    if (modelIds.Contains(model.Id))
                    {
                        model.Count--;
                        shouldUpdateSimulation = true;
                    }
                }
            }

            if (shouldUpdateSimulation)
            {
                if (this.running)
                {
                    this.log.Info("Deleting devices from running simulation", () => { });
                    await this.runner.DeleteDevices(ids);
                }
                else
                {
                    this.log.Info("Deleting devices from hub", () => { });
                    await this.simulations.DeleteDevicesAsync(ids);
                }

                await this.simulations.UpsertAsync(simulation);
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

                if (this.simulation.ShouldBeRunning())
                {
                    this.log.Debug("------ Starting simulation ------", () => new { this.simulation });
                    this.runner.Start(this.simulation);
                    this.log.Debug("------ Simulation started ------", () => new { this.simulation });
                }
            }
        }

        private void CheckForNewSimulation(Services.Models.Simulation newSimulation)
        {
            if (newSimulation != null && this.simulation == null)
            {
                this.simulation = newSimulation;
                if (this.simulation.ShouldBeRunning())
                {
                    this.log.Debug("------ Starting new simulation ------", () => new { this.simulation });
                    this.runner.Start(this.simulation);
                    this.log.Debug("------ New simulation started ------", () => new { this.simulation });
                }
            }
        }

        private void CheckForStopOrStartToSimulation()
        {
            // stopped
            if (this.simulation != null && !this.simulation.ShouldBeRunning())
            {
                this.runner.Stop();
            }

            // started
            if (this.simulation != null && this.simulation.ShouldBeRunning())
            {
                this.runner.Start(this.simulation);
            }
        }

        private List<string> parseModelIds(List<string> ids)
        {
            return ids.Select(s => s.Substring(0, s.LastIndexOf(('.')))).ToList<string>();
        }

        private string parsePosition(string id)
        {
            int index = id.LastIndexOf(('.')) + 1;
            return id.Substring(index, id.Length - index);
        }
    }
}
