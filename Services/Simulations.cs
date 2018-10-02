// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface ISimulations
    {
        // Get list of simulations.
        Task<IList<Models.Simulation>> GetListAsync();

        // Get a simulation.
        Task<Models.Simulation> GetAsync(string id);

        // Create a simulation.
        Task<Models.Simulation> InsertAsync(Models.Simulation simulation, string template = "");

        // Create or Replace a simulation.
        Task<Models.Simulation> UpsertAsync(Models.Simulation simulation);

        // Modify a simulation.
        Task<Models.Simulation> MergeAsync(SimulationPatch patch);

        // Add a device to simulation
        Task AddDeviceAsync(string id);

        // Delete a simulation and its devices.
        Task DeleteAsync(string id);

        // Try to start a job to create all the devices
        Task<bool> TryToStartDevicesCreationAsync(string simulationId, IDevices devices);

        // Change the simulation, setting the device creation complete
        Task<bool> TryToSetDeviceCreationCompleteAsync(string simulationId);

        // Get the ID of the devices in a simulation.
        IEnumerable<string> GetDeviceIds(Models.Simulation simulation);

        // Get the ID of the devices in a simulation, grouped by device model ID.
        Dictionary<string, List<string>> GetDeviceIdsByModel(Models.Simulation simulation);
    }

    public class Simulations : ISimulations
    {
        private const string DEFAULT_SIMULATION_ID = "1";
        private const string DEFAULT_TEMPLATE_NAME = "default";
        private const string DEVICES_COLLECTION = "SimulatedDevices";
        private const int DEVICES_PER_MODEL_IN_DEFAULT_TEMPLATE = 1;

        private readonly IDeviceModels deviceModels;
        private readonly IStorageAdapterClient storageAdapterClient;
        private readonly IStorageRecords simulationsStorage;
        private readonly IIotHubConnectionStringManager connectionStringManager;
        private readonly IDevices devices;
        private readonly ILogger log;
        private readonly IDiagnosticsLogger diagnosticsLogger;

        public Simulations(
            IServicesConfig config,
            IDeviceModels deviceModels,
            IFactory factory,
            IStorageAdapterClient storageAdapterClient,
            IIotHubConnectionStringManager connectionStringManager,
            IDevices devices,
            ILogger logger,
            IDiagnosticsLogger diagnosticsLogger)
        {
            this.deviceModels = deviceModels;
            this.storageAdapterClient = storageAdapterClient;
            this.simulationsStorage = factory.Resolve<IStorageRecords>().Init(config.SimulationsStorage);
            this.connectionStringManager = connectionStringManager;
            this.devices = devices;
            this.log = logger;
            this.diagnosticsLogger = diagnosticsLogger;
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

            // TODO: This will need changes to support pagination. Also order should be by simulation Id.
            return result.OrderByDescending(s => s.Created).ToList();
        }

        /// <summary>
        /// Get a simulation.
        /// </summary>
        public async Task<Models.Simulation> GetAsync(string id)
        {
            var item = await this.simulationsStorage.GetAsync(id);
            if (item == null) return null;

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
            var usingDefaultTemplate = !string.IsNullOrEmpty(template) && template.ToLowerInvariant() == DEFAULT_TEMPLATE_NAME;

            if (!string.IsNullOrEmpty(template) && template.ToLowerInvariant() != DEFAULT_TEMPLATE_NAME)
            {
                this.log.Warn("Unknown template name", () => new { template });
                throw new InvalidInputException("Unknown template name. Try 'default'.");
            }

            if (!usingDefaultTemplate && string.IsNullOrEmpty(simulation.Name))
            {
                this.log.Warn("Missing simulation name", () => new { });
                throw new InvalidInputException("Simulation name is required.");
            }

            // Note: forcing the ID because only one simulation can be created
            simulation.Id = usingDefaultTemplate ? DEFAULT_SIMULATION_ID : Guid.NewGuid().ToString();
            simulation.Created = DateTimeOffset.UtcNow;

            // Create default simulation
            if (usingDefaultTemplate)
            {
                simulation.Name = "Default Simulation";
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

                simulation.IotHubConnectionStrings = new List<string> { ServicesConfig.USE_DEFAULT_IOTHUB };
            }

            for (var index = 0; index < simulation.IotHubConnectionStrings.Count; index++)
            {
                var connString = await this.connectionStringManager.RedactAndSaveAsync(simulation.IotHubConnectionStrings[index]);

                if (!simulation.IotHubConnectionStrings.Contains(connString))
                {
                    simulation.IotHubConnectionStrings[index] = connString;
                }
            }

            // This value cannot be set by the user, we set it here and make sure it's "false"
            simulation.PartitioningComplete = false;

            return await this.SaveAsync(simulation, "*");
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

            Models.Simulation existingSimulation = null;

            try
            {
                existingSimulation = await this.GetAsync(simulation.Id);
            }
            catch (ResourceNotFoundException)
            {
                // Do nothing - this mean simulation does not exist in storage
                // Therefore allow to create a new simulation
                this.log.Info("Simulation not found in storage, hence proceeding to create new simulation");
            }

            if (existingSimulation != null)
            {
                this.log.Info("Modifying simulation");

                if (simulation.ETag == "*")
                {
                    simulation.ETag = existingSimulation.ETag;
                    this.log.Warn("The client used ETag='*' choosing to overwrite the current simulation");
                }

                if (simulation.ETag != existingSimulation.ETag)
                {
                    this.log.Error("Invalid simulation ETag",
                        () => new { simulation.Id, ETagInStorage = existingSimulation.ETag, ETagInRequest = simulation.ETag });
                    this.diagnosticsLogger.LogServiceError($"Invalid simulation ETag", new { simulation.Id, simulation.Name });
                    throw new ResourceOutOfDateException($"Invalid ETag. The simulation ETag is '{existingSimulation.ETag}'");
                }

                simulation.Created = existingSimulation.Created;
            }
            else
            {
                this.log.Info("Creating new simulation");

                simulation.Created = DateTimeOffset.UtcNow;

                // This value cannot be set by the user, we set it here and make sure it's "false"
                simulation.PartitioningComplete = false;
            }

            for (var index = 0; index < simulation.IotHubConnectionStrings.Count; index++)
            {
                var connString = await this.connectionStringManager.RedactAndSaveAsync(simulation.IotHubConnectionStrings[index]);

                if (!simulation.IotHubConnectionStrings.Contains(connString))
                {
                    simulation.IotHubConnectionStrings[index] = connString;
                }
            }

            return await this.SaveAsync(simulation, simulation.ETag);
        }

        /// <summary>
        /// Modify a simulation.
        /// </summary>
        public async Task<Models.Simulation> MergeAsync(SimulationPatch patch)
        {
            if (string.IsNullOrEmpty(patch.Id))
            {
                this.log.Warn("Invalid simulation ID.", () => new { patch.Id });
                throw new InvalidInputException("Invalid simulation ID.");
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

            if (patch.Enabled == false)
            {
                simulation.StoppedTime = simulation.Modified;
                simulation.Statistics = new Models.Simulation.StatisticsRef
                {
                    AverageMessagesPerSecond = patch.Statistics.AverageMessagesPerSecond,
                    TotalMessagesSent = patch.Statistics.TotalMessagesSent
                };

                // When a simulation is disabled, its partitions are deleted - this triggers the deletion
                if (!simulation.Enabled)
                {
                    simulation.PartitioningComplete = false;
                }
            }

            item = await this.simulationsStorage.UpsertAsync(
                new StorageRecord
                {
                    Id = simulation.Id,
                    Data = JsonConvert.SerializeObject(simulation)
                },
                patch.ETag
            );

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

            // Then delete the simulation from storage
            await this.simulationsStorage.DeleteAsync(id);
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

        public async Task AddDeviceAsync(string id)
        {
            await this.storageAdapterClient.CreateAsync(DEVICES_COLLECTION, id, id);
        }

        /// <summary>
        /// Get the ID of the devices in a simulation.
        /// </summary>
        public IEnumerable<string> GetDeviceIds(Models.Simulation simulation)
        {
            var deviceIds = new List<string>();

            // Calculate the device IDs used in the simulation
            var models = (from model in simulation.DeviceModels where model.Count > 0 select model).ToList();
            foreach (var model in models)
            {
                for (var i = 0; i < model.Count; i++)
                {
                    deviceIds.Add(this.devices.GenerateId(model.Id, i));
                }
            }

            return deviceIds;
        }

        public Dictionary<string, List<string>> GetDeviceIdsByModel(Models.Simulation simulation)
        {
            var result = new Dictionary<string, List<string>>();
            var deviceCount = 0;

            // Load the simulation models with at least 1 device to simulate (ignoring the custom device ID for now)
            List<Models.Simulation.DeviceModelRef> models = (from model in simulation.DeviceModels where model.Count > 0 select model).ToList();

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

            // Add custom device IDs
            foreach (var device in simulation.CustomDevices)
            {
                if (!result.ContainsKey(device.DeviceModel.Id))
                {
                    result.Add(device.DeviceModel.Id, new List<string>());
                }

                result[device.DeviceModel.Id].Add(device.DeviceId);
                deviceCount++;
            }

            this.log.Debug("Device IDs loaded", () => new { Simulation = simulation.Id, deviceCount });

            return result;
        }

        private async Task<Models.Simulation> SaveAsync(Models.Simulation simulation, string eTag)
        {
            simulation.Modified = DateTimeOffset.UtcNow;

            // When a simulation is disabled, its partitions are deleted - this triggers the deletion
            if (!simulation.Enabled)
            {
                simulation.PartitioningComplete = false;
            }

            var result = await this.simulationsStorage.UpsertAsync(
                new StorageRecord
                {
                    Id = simulation.Id,
                    Data = JsonConvert.SerializeObject(simulation)
                },
                eTag
            );

            // Use the new ETag provided by the storage
            simulation.ETag = result.ETag;
            simulation.Id = result.Id;

            this.log.Info("Simulation written to storage",
                () => new
                {
                    simulation.Id,
                    simulation.Enabled,
                    simulation.PartitioningComplete
                });

            return simulation;
        }

        // Generate a device Id
        private string GenerateId(string simulationId, string deviceModelId, int position)
        {
            return simulationId + "." + deviceModelId + "." + position;
        }
    }
}
