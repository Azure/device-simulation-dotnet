// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DeviceModels;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
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

        /*
        /// <summary>
        /// Delete a simulation and its devices.
        /// </summary>
        Task DeleteAsync(string id);

        /// <summary>
        /// Get the ID of the devices in a simulation.
        /// </summary>
        IList<string> GetDeviceIds(Models.Simulation simulation);
        */

        /// <summary>
        /// Get the ID of the devices in a simulation, organized by device model ID.
        /// </summary>
        Dictionary<string, List<string>> GetDeviceIdsByModel(Models.Simulation simulation);
    }

    // Note: singleton class
    public class Simulations : ISimulations
    {
        private const string SIMULATION_ID = "1";
        private const int DEVICES_PER_MODEL_IN_DEFAULT_TEMPLATE = 1;

        private readonly IDeviceModels deviceModels;
        private readonly IIotHubConnectionStringManager connectionStringManager;
        private readonly IStorageRecords simulationsStorage;
        private readonly ILogger log;

        public Simulations(
            IServicesConfig config,
            IDeviceModels deviceModels,
            IFactory factory,
            IIotHubConnectionStringManager connectionStringManager,
            ILogger logger)
        {
            this.deviceModels = deviceModels;
            this.simulationsStorage = factory.Resolve<IStorageRecords>().Init(config.SimulationsStorage);
            this.connectionStringManager = connectionStringManager;
            this.log = logger;
        }

        /// <summary>
        /// Get list of simulations.
        /// </summary>
        public async Task<IList<Models.Simulation>> GetListAsync()
        {
            var items = await this.simulationsStorage.GetAllAsync();
            var result = new List<Models.Simulation>();
            foreach (var item in items)
            {
                var simulation = JsonConvert.DeserializeObject<Models.Simulation>(item.Data);
                simulation.ETag = item.ETag;
                simulation.Id = item.Id;
                result.Add(simulation);
            }

            return result;
        }

        /// <summary>
        /// Get a simulation.
        /// </summary>
        public async Task<Models.Simulation> GetAsync(string id)
        {
            var item = await this.simulationsStorage.GetAsync(id);
            var simulation = JsonConvert.DeserializeObject<Models.Simulation>(item.Data);
            simulation.ETag = item.ETag;
            simulation.Id = item.Id;
            return simulation;
        }

        /// <summary>
        /// Create a simulation.
        /// </summary>
        public async Task<Models.Simulation> InsertAsync(Models.Simulation simulation, string template = "")
        {
            // TODO: complete validation
            if (!string.IsNullOrEmpty(template) && template.ToLowerInvariant() != "default")
            {
                this.log.Warn("Unknown template name", () => new { template });
                throw new InvalidInputException("Unknown template name. Try 'default'.");
            }

            var existingSimulations = await this.GetListAsync();
            if (existingSimulations.Count > 0)
            {
                this.log.Warn("There is already a simulation");
                throw new ConflictingResourceException(
                    "There is already a simulation. Only one simulation can be created.");
            }

            // Note: forcing the ID because only one simulation can be created
            simulation.Id = SIMULATION_ID;
            simulation.Created = DateTimeOffset.UtcNow;
            simulation.Modified = simulation.Created;

            // Create default simulation
            if (!string.IsNullOrEmpty(template) && template.ToLowerInvariant() == "default")
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

            // This value cannot be set by the user, so we set it here
            simulation.PartitioningComplete = false;

            var result = await this.simulationsStorage.CreateAsync(
                new StorageRecord { Id = SIMULATION_ID, Data = JsonConvert.SerializeObject(simulation) });

            simulation.ETag = result.ETag;

            return simulation;
        }

        /// <summary>
        /// Create or Replace a simulation.
        /// The logic works under the assumption that there is only one simulation with id "1".
        /// </summary>
        public async Task<Models.Simulation> UpsertAsync(Models.Simulation simulation)
        {
            if (simulation.Id != SIMULATION_ID)
            {
                this.log.Warn("Invalid simulation ID. Only one simulation is allowed");
                throw new InvalidInputException("Invalid simulation ID. Use ID '" + SIMULATION_ID + "'.");
            }

            var simulations = await this.GetListAsync();
            if (simulations.Count > 0)
            {
                this.log.Info("Modifying simulation");

                if (simulation.ETag == "*")
                {
                    simulation.ETag = simulations[0].ETag;
                    this.log.Info("The client used ETag='*' choosing to overwrite the current simulation");
                }

                if (simulation.ETag != simulations[0].ETag)
                {
                    this.log.Error("Invalid ETag. Running simulation ETag is:'", () => new { simulations });
                    throw new ConflictingResourceException("Invalid ETag. Running simulation ETag is:'" + simulations[0].ETag + "'.");
                }

                simulation.Created = simulations[0].Created;
                simulation.Modified = DateTimeOffset.UtcNow;

                // When a simulation is disabled, its partitions are deleted
                if (!simulation.Enabled)
                {
                    simulation.PartitioningComplete = false;
                }
            }
            else
            {
                this.log.Info("Creating new simulation");

                // new simulation
                simulation.Created = DateTimeOffset.UtcNow;
                simulation.Modified = simulation.Created;

                // This value cannot be set by the user, so we set it here
                simulation.PartitioningComplete = false;
            }

            // Note: forcing the ID because only one simulation can be created
            simulation.Id = SIMULATION_ID;

            // TODO if write to storage adapter fails, the iothub connection string 
            //      will still be stored to disk. Storing the encrypted string using
            //      storage adapter would address this
            //      https://github.com/Azure/device-simulation-dotnet/issues/129
            simulation.IotHubConnectionString = await this.connectionStringManager.RedactAndStoreAsync(simulation.IotHubConnectionString);

            var result = await this.simulationsStorage.UpsertAsync(
                new StorageRecord { Id = SIMULATION_ID, Data = JsonConvert.SerializeObject(simulation) },
                simulation.ETag);

            // Use the new ETag provided by the storage
            simulation.ETag = result.ETag;

            return simulation;
        }

        /// <summary>
        /// Modify a simulation.
        /// </summary>
        public async Task<Models.Simulation> MergeAsync(SimulationPatch patch)
        {
            if (patch.Id != SIMULATION_ID)
            {
                this.log.Warn("Invalid simulation ID.", () => new { patch.Id });
                throw new InvalidInputException("Invalid simulation ID. Use ID '" + SIMULATION_ID + "'.");
            }

            var item = await this.simulationsStorage.GetAsync(patch.Id);
            var simulation = JsonConvert.DeserializeObject<Models.Simulation>(item.Data);
            simulation.ETag = item.ETag;
            simulation.Id = item.Id;

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

            item = await this.simulationsStorage.UpsertAsync(
                new StorageRecord { Id = SIMULATION_ID, Data = JsonConvert.SerializeObject(simulation) },
                patch.ETag);

            // Use the new ETag provided by the storage
            simulation.ETag = item.ETag;

            return simulation;
        }

        /*
        /// <summary>
        /// Delete a simulation and its devices.
        /// </summary>
        public async Task DeleteAsync(string id)
        {
            // Delete devices first
            var deviceIds = this.GetDeviceIds(await this.GetAsync(id));
            await this.devices.DeleteListAsync(deviceIds);
            
            // Then delete the simulation from the storage
            await this.simulationsStorage.DeleteAsync(id);
        }
        
        /// <summary>
        /// Generate the list of device IDs. This list will eventually be retrieved from the database.
        /// </summary>
        public IList<string> GetDeviceIds(Models.Simulation simulation)
        {
            var result = new List<string>();
            
            // Calculate the device IDs used in the simulation
            var models = (from model in simulation.DeviceModels where model.Count > 0 select model).ToList();
            foreach (var model in models)
            {
                for (var i = 1; i <= model.Count; i++)
                {
                    result.Add(this.GenerateId(simulation.Id, model.Id, i));
                }
            }
            
            this.log.Debug("Device IDs loaded", () => new { Simulation = simulation.Id, result.Count });
            
            return result;
        }
        */

        /// <summary>
        /// Generate the list of device IDs. This list will eventually be retrieved from the database.
        /// </summary>
        public Dictionary<string, List<string>> GetDeviceIdsByModel(Models.Simulation simulation)
        {
            var result = new Dictionary<string, List<string>>();
            var deviceCount = 0;

            // Load the simulation models with at least 1 device to simulate
            var models = (from model in simulation.DeviceModels where model.Count > 0 select model).ToList();

            // Generate ID, e.g. "1.chiller-01.1", "1.chiller-01.2", etc 
            foreach (var model in models)
            {
                var deviceIds = new List<string>();
                for (var i = 1; i <= model.Count; i++)
                {
                    deviceIds.Add(this.GenerateId(simulation.Id, model.Id, i));
                    deviceCount++;
                }

                result.Add(model.Id, deviceIds);
            }

            this.log.Debug("Device IDs loaded", () => new { Simulation = simulation.Id, deviceCount });

            return result;
        }

        // Generate a device Id
        private string GenerateId(string simulationId, string deviceModelId, int position)
        {
            return simulationId + "." + deviceModelId + "." + position;
        }
    }
}
