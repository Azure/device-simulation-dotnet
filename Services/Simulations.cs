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
        // Get list of simulations
        Task<IList<Models.Simulation>> GetListAsync();

        // Get a simulation
        Task<Models.Simulation> GetAsync(string id);

        // Create a simulation
        Task<Models.Simulation> InsertAsync(Models.Simulation simulation, string template = "");

        // Create or Replace a simulation
        Task<Models.Simulation> UpsertAsync(Models.Simulation simulation);

        // Modify some simulation details
        Task<Models.Simulation> MergeAsync(SimulationPatch patch);

        // Try to start a job to create all the devices
        Task<bool> TryToStartDevicesCreationAsync(string simulationId, IDevices devices);

        // Change the simulation, setting the device creation complete
        Task<bool> TryToSetDeviceCreationCompleteAsync(string simulationId);

        // Get the ID of the devices in a simulation, organized by device model ID.
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
                this.log.Debug("Modifying simulation");

                if (simulation.ETag == "*")
                {
                    simulation.ETag = simulations[0].ETag;
                    this.log.Warn("The client used ETag='*' choosing to overwrite the current simulation");
                }

                if (simulation.ETag != simulations[0].ETag)
                {
                    this.log.Error("Invalid ETag. Running simulation ETag is:'", () => new { simulations });
                    throw new ConflictingResourceException("Invalid ETag. Running simulation ETag is:'" + simulations[0].ETag + "'.");
                }

                simulation.Created = simulations[0].Created;

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

            return await this.SaveAsync(simulation, simulation.ETag);
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

            var simulation = await this.GetAsync(patch.Id);

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

            return await this.SaveAsync(simulation, patch.ETag);
        }

        public async Task<bool> TryToStartDevicesCreationAsync(string simulationId, IDevices devices)
        {
            // Fetch latest record
            var simulation = await this.GetAsync(simulationId);

            // Edit the record only if required
            if (!simulation.DevicesCreationStarted)
            {
                try
                {
                    Dictionary<string, List<string>> deviceList = this.GetDeviceIdsByModel(simulation);
                    var deviceIds = deviceList.SelectMany(x => x.Value);
                    this.log.Info("Creating devices...", () => new { simulationId });

                    simulation.DeviceCreationJobId = await devices.CreateListUsingJobsAsync(deviceIds);
                    simulation.DevicesCreationStarted = true;

                    this.log.Info("Device import job created", () => new { simulationId, simulation.DeviceCreationJobId });

                    await this.SaveAsync(simulation, simulation.ETag);
                }
                catch (Exception e)
                {
                    this.log.Error("Failed to create device import job", e);
                    return false;
                }
            }

            return true;
        }

        // Change the simulation, setting the device creation complete
        public async Task<bool> TryToSetDeviceCreationCompleteAsync(string simulationId)
        {
            var simulation = await this.GetAsync(simulationId);

            // Edit the record only if required
            if (simulation.DevicesCreationComplete) return true;
            
            try
            {
                simulation.DevicesCreationComplete = true;
                await this.SaveAsync(simulation, simulation.ETag);
            }
            catch (ConflictingResourceException e)
            {
                this.log.Warn("Update failed, another client modified the simulation record", e);
                return false;
            }

            return true;
        }

        private async Task<Models.Simulation> SaveAsync(Models.Simulation simulation, string eTag)
        {
            simulation.Modified = DateTimeOffset.UtcNow;

            var result = await this.simulationsStorage.UpsertAsync(
                new StorageRecord { Id = simulation.Id, Data = JsonConvert.SerializeObject(simulation) },
                eTag);

            this.log.Info("Simulation written to storage",
                () => new
                {
                    simulation.Id, simulation.Enabled,
                    simulation.PartitioningComplete, simulation.DevicesCreationStarted, simulation.DevicesCreationComplete
                });

            // Use the new ETag provided by the storage
            simulation.ETag = result.ETag;

            return simulation;
        }

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
