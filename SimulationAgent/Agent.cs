// Copyright (c) Microsoft. All rights reserved. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    public interface ISimulationAgent
    {
        Task RunAsync();
        Task AddDeviceAsync(string name, string modelId);
        Task DeleteDevicesAsync(List<string> ids);
        void Stop();
    }

    public class Agent : ISimulationAgent
    {
        private const int CHECK_INTERVAL_MSECS = 10000;

        private readonly ILogger log;
        private readonly ISimulations simulations;
        private readonly ISimulationRunner runner;
        private readonly IDeviceModels deviceModels;
        private Services.Models.Simulation simulation;
        private bool running;

        public Agent(
            ILogger logger,
            ISimulations simulations,
            ISimulationRunner runner,
            IDeviceModels deviceModels)
        {
            this.log = logger;
            this.simulations = simulations;
            this.runner = runner;
            this.deviceModels = deviceModels;
            this.running = true;
        }

        public async Task RunAsync()
        {
            this.log.Info("Simulation Agent running");

            // Keep running, checking if the simulation changes
            while (this.running)
            {
                var oldSimulation = this.simulation;
                try
                {
                    this.log.Debug("------ Checking for simulation changes ------");

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
                    this.log.Error("Failure reading and starting simulation from storage.", e);
                    this.simulation = oldSimulation;
                }

                if (this.simulation != null && this.simulation.ShouldBeRunning())
                {
                    this.log.Debug("------ Current simulation being run ------");
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
            this.simulation = null;
            this.running = false;
            this.runner.Stop();
        }

        public async Task AddDeviceAsync(string deviceId, string modelId)
        {
            if (this.simulation != null && this.IsDeviceModelIdValidAsync(modelId))
            {
                try
                {
                    await this.AddDeviceToSimulationRecordAsync(this.simulation, deviceId, modelId);

                    if (this.running)
                    {
                        this.log.Info("Add device to running simulation");
                        await this.runner.AddDeviceAsync(deviceId, modelId);
                    }
                    else
                    {
                        this.log.Info("Add device to IoT Hub");
                        await this.simulations.AddDeviceAsync(deviceId);
                    }
                }
                catch (Exception e)
                {
                    this.log.Debug("Error while adding new device", () => new { e });
                    throw new Exception("Error while adding a new device");
                }
            }
        }

        public async Task DeleteDevicesAsync(List<string> ids)
        {
            this.log.Info("Update simulation");

            try
            {
                if (this.simulation != null)
                {
                    await this.DeleteDevicesFromSimulationRecordAsync(this.simulation, ids);

                    if (this.running)
                    {
                        this.log.Info("Deleting devices from running simulation");
                        await this.runner.DeleteDevicesAsync(ids);
                    }
                    else
                    {
                        this.log.Info("Deleting devices from hub");
                        await this.simulations.DeleteDevicesAsync(ids);
                    }
                }
            }
            catch (Exception e)
            {
                this.log.Debug("Error while deleting new device", () => new { e });
                throw new Exception("Error while deleting a new device");
            }
        }

        private void CheckForDeletedSimulation(Services.Models.Simulation newSimulation)
        {
            if (newSimulation == null && this.simulation != null)
            {
                this.runner.Stop();
                this.simulation = null;
                this.log.Debug("No simulation found in storage...");
            }
        }

        private void CheckForChangedSimulation(Services.Models.Simulation newSimulation)
        {
            if (newSimulation != null && this.simulation != null &&
                newSimulation.Modified != this.simulation.Modified)
            {
                this.log.Debug("The simulation has been modified, stopping the current " +
                               "simulation and starting the new one if enabled");
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

        private bool IsDeviceModelIdValidAsync(string modelId)
        {
            var models = this.deviceModels.GetListAsync().Result;

            foreach (var model in models)
            {
                if (modelId.Equals(model.Id, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task AddDeviceToSimulationRecordAsync(Simulation simulation, string deviceId, string modelId)
        {
            DeviceModelRef deviceModel = new DeviceModelRef();
            deviceModel.Id = modelId;
            CustomDeviceRef customDevice = new CustomDeviceRef();
            customDevice.DeviceModel = deviceModel;
            customDevice.DeviceId = deviceId;

            try
            {
                if (!simulation.CustomDevices.ToList().Contains(customDevice))
                {
                    this.log.Info("Update simulation record");
                    simulation.CustomDevices.Add(customDevice);
                    await this.simulations.UpsertAsync(simulation);
                }
                else
                {
                    this.log.Debug("Device already exists in simulation record");
                }
            }
            catch (Exception e)
            {
                this.log.Error("Error while adding new device to simulation record", () => new { e });
                throw new Exception("Error while adding a new device to simulation record");
            }
        }

        private async Task DeleteDevicesFromSimulationRecordAsync(Simulation simulation, List<string> ids)
        {
            bool shouldUpdateSimulation = false;

            foreach (var id in ids.ToList())
            {
                var deviceRemoved = false;

                // Try to remove device from custom devices list
                foreach (var model in simulation.CustomDevices.ToList())
                {
                    if (id.Equals(model.DeviceId))
                    {
                        this.log.Info("Remove device from custom device list", () => new { id });
                        simulation.CustomDevices.Remove(model);
                        shouldUpdateSimulation = true;
                        deviceRemoved = true;
                        break;
                    }
                }

                if (!deviceRemoved)
                {
                    // Try to remove device from device models list. Update device model count.
                    foreach (var model in simulation.DeviceModels.ToList())
                    {
                        string parsedModelId = id.Substring(0, id.LastIndexOf(('.')));
                        if (parsedModelId.Equals(model.Id))
                        {
                            this.log.Info("Decrement device model count", () => new { id, parsedModelId });
                            model.Count--;
                            if (model.Count <= 0)
                            {
                                this.log.Info("Remove device from device model list", () => new { id });
                                simulation.DeviceModels.Remove(model);
                            }

                            shouldUpdateSimulation = true;
                        }
                    }
                }
            }

            if (shouldUpdateSimulation)
            {
                this.log.Info("Update simulation record in storage");
                await this.simulations.UpsertAsync(simulation);
            }
        }
    }
}
