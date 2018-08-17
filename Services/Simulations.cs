// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface ISimulations
    {
        /// <summary>
        /// Get list of simulations.
        /// </summary>
        Task<IList<Models.Simulation>> GetListAsync();

        /// <summary>
        /// Get a simulation.
        /// </summary>
        Task<Models.Simulation> GetAsync(string id);

        /// <summary>
        /// Create a simulation.
        /// </summary>
        Task<Models.Simulation> InsertAsync(Models.Simulation simulation, string template = "");

        /// <summary>
        /// Create or Replace a simulation.
        /// </summary>
        Task<Models.Simulation> UpsertAsync(Models.Simulation simulation);

        /// <summary>
        /// Modify a simulation.
        /// </summary>
        Task<Models.Simulation> MergeAsync(SimulationPatch patch);

        /// <summary>
        /// Delete a simulation and its devices.
        /// </summary>
        Task DeleteAsync(string id);

        /// <summary>
        /// Get the ID of the devices in a simulation.
        /// </summary>
        IEnumerable<string> GetDeviceIds(Models.Simulation simulation);
    }

    public class Simulations : ISimulations
    {
        private const string STORAGE_COLLECTION = "simulations";
        private const string DEFAULT_SIMULATION_ID = "1";
        private const int DEVICES_PER_MODEL_IN_DEFAULT_TEMPLATE = 1;

        private readonly IDeviceModels deviceModels;
        private readonly IStorageAdapterClient storage;
        private readonly IIotHubConnectionStringManager connectionStringManager;
        private readonly IDevices devices;
        private readonly ILogger log;

        public Simulations(
            IDeviceModels deviceModels,
            IStorageAdapterClient storage,
            IIotHubConnectionStringManager connectionStringManager,
            IDevices devices,
            ILogger logger)
        {
            this.deviceModels = deviceModels;
            this.storage = storage;
            this.connectionStringManager = connectionStringManager;
            this.devices = devices;
            this.log = logger;
        }

        /// <summary>
        /// Get list of simulations.
        /// </summary>
        public async Task<IList<Models.Simulation>> GetListAsync()
        {
            var data = await this.storage.GetAllAsync(STORAGE_COLLECTION);
            var result = new List<Models.Simulation>();
            foreach (var item in data.Items)
            {
                var simulation = JsonConvert.DeserializeObject<Models.Simulation>(item.Data);
                simulation.ETag = item.ETag;
                result.Add(simulation);
            }

            return result.OrderByDescending(s => s.Modified).ToList();
        }

        /// <summary>
        /// Get a simulation.
        /// </summary>
        public async Task<Models.Simulation> GetAsync(string id)
        {
            var item = await this.storage.GetAsync(STORAGE_COLLECTION, id);

            if (item != null)
            {
                var simulation = JsonConvert.DeserializeObject<Models.Simulation>(item.Data);
                simulation.ETag = item.ETag;
                return simulation;
            }

            return null;
        }

        /// <summary>
        /// Create a simulation.
        /// </summary>
        public async Task<Models.Simulation> InsertAsync(Models.Simulation simulation, string template = "")
        {
            var usingDefaultTemplate = !string.IsNullOrEmpty(template) && template.ToLowerInvariant() == "default";

            // TODO: complete validation
            if (!string.IsNullOrEmpty(template) && template.ToLowerInvariant() != "default")
            {
                this.log.Warn("Unknown template name", () => new { template });
                throw new InvalidInputException("Unknown template name. Try 'default'.");
            }

            if (!usingDefaultTemplate && string.IsNullOrEmpty(simulation.Name))
            {
                this.log.Warn("Missing simulation name", () => new { });
                throw new InvalidInputException("Simulation name is required.");
            }

            var simulations = await this.GetListAsync();
            var activeSimulation = simulations.Where(a => a.ShouldBeRunning());
            if (activeSimulation.Any())
            {
                this.log.Warn("There is already a running simulation", () => { });
                throw new ConflictingResourceException(
                    "There is already a simulation. Simulation cannot be created while another simulation is running.");
            }

            // Note: forcing the ID because only one simulation can be created
            simulation.Id = usingDefaultTemplate ? DEFAULT_SIMULATION_ID : Guid.NewGuid().ToString();
            simulation.Created = DateTimeOffset.UtcNow;
            simulation.Modified = simulation.Created;

            // Create default simulation
            if (usingDefaultTemplate)
            {
                var types = await this.deviceModels.GetListAsync();
                simulation.DeviceModels = new List<Models.Simulation.DeviceModelRef>();
                foreach (var type in types)
                {
                    simulation.DeviceModels.Add(new Models.Simulation.DeviceModelRef
                    {
                        Id = type.Id,
                        Count = DEVICES_PER_MODEL_IN_DEFAULT_TEMPLATE
                    });
                }
            }

            // TODO if write to storage adapter fails, the iothub connection string 
            //      will still be stored to disk. Storing the encrypted string using
            //      storage adapter would address this
            //      https://github.com/Azure/device-simulation-dotnet/issues/129
            simulation.IotHubConnectionString = await this.connectionStringManager.RedactAndStoreAsync(simulation.IotHubConnectionString);

            // Note: using UpdateAsync because the service generates the ID

            var result = await this.storage.UpdateAsync(
                STORAGE_COLLECTION,
                simulation.Id,
                JsonConvert.SerializeObject(simulation),
                "*");

            simulation.ETag = result.ETag;

            return simulation;
        }

        /// <summary>
        /// Create or Replace a simulation.
        /// The logic works under the assumption that there is only one simulation with id "1".
        /// </summary>
        public async Task<Models.Simulation> UpsertAsync(Models.Simulation simulation)
        {
            if (string.IsNullOrEmpty(simulation.Id))
            {
                throw new InvalidInputException("Simulation ID is not specified.");
            }

            var existingSimulation = await this.GetAsync(simulation.Id);
            if (existingSimulation != null)
            {
                this.log.Info("Modifying simulation via PUT.", () => { });

                if (simulation.ETag == "*")
                {
                    simulation.ETag = existingSimulation.ETag;
                    this.log.Info("The client used ETag='*' choosing to overwrite the current simulation", () => { });
                }

                if (simulation.ETag != existingSimulation.ETag)
                {
                    this.log.Error("Invalid ETag. Running simulation ETag is:'", () => new { simulation });
                    throw new ResourceOutOfDateException("Invalid ETag. Running simulation ETag is:'" + simulation.ETag + "'.");
                }

                simulation.Created = existingSimulation.Created;
                simulation.Modified = DateTimeOffset.UtcNow;
                simulation.TotalMessagesSent = existingSimulation.TotalMessagesSent;
                simulation.AverageMessagesSent = existingSimulation.AverageMessagesSent;
            }
            else
            {
                this.log.Info("Creating new simulation via PUT.", () => { });
                // new simulation
                simulation.Created = DateTimeOffset.UtcNow;
                simulation.Modified = simulation.Created;
            }

            // TODO if write to storage adapter fails, the iothub connection string 
            //      will still be stored to disk. Storing the encrypted string using
            //      storage adapter would address this
            //      https://github.com/Azure/device-simulation-dotnet/issues/129
            simulation.IotHubConnectionString = await this.connectionStringManager.RedactAndStoreAsync(simulation.IotHubConnectionString);

            var result = await this.storage.UpdateAsync(
                STORAGE_COLLECTION,
                simulation.Id,
                JsonConvert.SerializeObject(simulation),
                simulation.ETag);

            // Return the new ETag provided by the storage
            simulation.ETag = result.ETag;

            return simulation;
        }

        /// <summary>
        /// Modify a simulation.
        /// </summary>
        public async Task<Models.Simulation> MergeAsync(SimulationPatch patch)
        {
            var item = await this.storage.GetAsync(STORAGE_COLLECTION, patch.Id);
            var simulation = JsonConvert.DeserializeObject<Models.Simulation>(item.Data);
            simulation.ETag = item.ETag;

            // Even when there's nothing to do, verify the ETag mismatch
            if (patch.ETag != simulation.ETag)
            {
                this.log.Warn("ETag mismatch",
                    () => new { Current = simulation.ETag, Provided = patch.ETag });
                throw new ConflictingResourceException(
                    $"The ETag provided doesn't match the current resource ETag ({simulation.ETag}).");
            }

            if (!patch.Enabled.HasValue || patch.Enabled.Value == simulation.Enabled)
            {
                // Nothing to do
                return simulation;
            }

            simulation.Enabled = patch.Enabled.Value;
            simulation.Modified = DateTimeOffset.UtcNow;

            if (patch.Enabled == false)
            {
                simulation.StopTime = simulation.Modified;
                simulation.TotalMessagesSent = patch.TotalMessagesSent;
                simulation.AverageMessagesSent = patch.AverageMessagesSent;
            }

            item = await this.storage.UpdateAsync(
                STORAGE_COLLECTION,
                simulation.Id,
                JsonConvert.SerializeObject(simulation),
                patch.ETag);

            simulation.ETag = item.ETag;

            return simulation;
        }

        /// <summary>
        /// Delete a simulation and its devices.
        /// </summary>
        public async Task DeleteAsync(string id)
        {
            // Delete devices first
            var deviceIds = this.GetDeviceIds(await this.GetAsync(id));
            await this.devices.DeleteListAsync(deviceIds);

            // Then delete the simulation from the storage
            await this.storage.DeleteAsync(STORAGE_COLLECTION, id);
        }

        /// <summary>
        /// Get the ID of the devices in a simulation.
        /// </summary>
        public IEnumerable<string> GetDeviceIds(Models.Simulation simulation)
        {
            var deviceIds = new List<string>();
            var simulationId = simulation.Id;

            // Calculate the device IDs used in the simulation
            var models = (from model in simulation.DeviceModels where model.Count > 0 select model).ToList();
            foreach (var model in models)
            {
                for (var i = 0; i < model.Count; i++)
                {
                    deviceIds.Add(this.devices.GenerateId(simulationId, model.Id, i));
                }
            }

            return deviceIds;
        }
    }
}
