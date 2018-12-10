// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Statistics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface ISimulations
    {
        // Get list of simulations.
        // This will have better perf than GetListWithStatisticsAsync.
        Task<IList<Models.Simulation>> GetListAsync();

        // Get list of simulations with statistics.
        Task<IList<Models.Simulation>> GetListWithStatisticsAsync();

        // Get a simulation.
        // This will have better perf than GetWithStatisticsAsync.
        Task<Models.Simulation> GetAsync(string id);

        // Get a simulation with statistics.
        Task<Models.Simulation> GetWithStatisticsAsync(string id);

        // Create a simulation.
        Task<Models.Simulation> InsertAsync(Models.Simulation simulation, string template = "");

        // Create or Replace a simulation.
        Task<Models.Simulation> UpsertAsync(Models.Simulation simulation, bool validateHubCredentials);

        // Modify a simulation.
        Task<Models.Simulation> MergeAsync(SimulationPatch patch);

        // Add a device to simulation
        Task AddDeviceAsync(string id);

        // Delete a simulation and its statistics (not the devices).
        Task DeleteAsync(string id);

        // Seed default simulations
        Task TrySeedAsync();

        // Try to start a job to create all the devices
        Task<bool> TryToStartDevicesCreationAsync(string simulationId, IDevices devices);

        // Change the simulation, setting the device creation complete
        Task<bool> TryToSetDeviceCreationCompleteAsync(string simulationId);

        // Create a job to delete all the devices (used by web service on user request)
        Task<(bool jobCreated, string jobId)> DeleteAllDevicesAsync(string simulationId, IDevices devices);

        // Try to start a job to delete all the devices (used by simulation agent when a simulation ends)
        Task<bool> TryToStartDevicesDeletionAsync(string simulationId, IDevices devices);

        // Change the simulation, setting the device deletion complete
        Task<bool> TryToSetDeviceDeletionCompleteAsync(string simulationId);

        // Get the ID of the devices in a simulation.
        IEnumerable<string> GetDeviceIds(Models.Simulation simulation);

        // Get the ID of the devices in a simulation, grouped by device model ID.
        Dictionary<string, List<string>> GetDeviceIdsByModel(Models.Simulation simulation);

        // Generate a device Id
        string GenerateId(string simulationId, string deviceModelId, int position);
    }

    public class Simulations : ISimulations
    {
        private const string DEFAULT_SIMULATION_ID = "1";
        private const string DEFAULT_TEMPLATE_NAME = "default";
        private const string DEVICES_COLLECTION = "SimulatedDevices";
        private const string SEED_STATUS_KEY = "SeedCompleted";
        private const int DEVICES_PER_MODEL_IN_DEFAULT_TEMPLATE = 1;

        private readonly IServicesConfig config;
        private readonly IDeviceModels deviceModels;
        private readonly IStorageAdapterClient storageAdapterClient;
        private readonly IEngine mainStorage;
        private readonly IEngine simulationsStorage;
        private readonly IConnectionStrings connectionStrings;
        private readonly ISimulationStatistics simulationStatistics;
        private readonly IFileSystem fileSystem;
        private readonly ILogger log;
        private readonly IDiagnosticsLogger diagnosticsLogger;

        public Simulations(
            IServicesConfig config,
            IDeviceModels deviceModels,
            IEngines engines,
            IStorageAdapterClient storageAdapterClient,
            IConnectionStrings connectionStrings,
            IFileSystem fileSystem,
            ILogger logger,
            IDiagnosticsLogger diagnosticsLogger,
            ISimulationStatistics simulationStatistics)
        {
            this.config = config;
            this.deviceModels = deviceModels;
            this.storageAdapterClient = storageAdapterClient;
            this.mainStorage = engines.Build(config.MainStorage);
            this.simulationsStorage = engines.Build(config.SimulationsStorage);
            this.connectionStrings = connectionStrings;
            this.fileSystem = fileSystem;
            this.log = logger;
            this.diagnosticsLogger = diagnosticsLogger;
            this.simulationStatistics = simulationStatistics;
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
                var simulation = JsonConvert.DeserializeObject<Models.Simulation>(item.GetData());
                simulation.ETag = item.GetETag();
                simulation.Id = item.GetId();
                result.Add(simulation);
            }

            // TODO: This will need changes to support pagination. Also order should be by simulation Id.
            return result.OrderByDescending(s => s.Created).ToList();
        }

        /// <summary>
        /// Get list of simulations with statistics.
        /// </summary>
        public async Task<IList<Models.Simulation>> GetListWithStatisticsAsync()
        {
            var items = await this.GetListAsync();

            foreach (var item in items)
            {
                item.Statistics = await this.simulationStatistics.GetSimulationStatisticsAsync(item.Id);
            }

            return items;
        }

        /// <summary>
        /// Get a simulation.
        /// </summary>
        public async Task<Models.Simulation> GetAsync(string id)
        {
            var item = await this.simulationsStorage.GetAsync(id);
            if (item == null) return null;

            var simulation = JsonConvert.DeserializeObject<Models.Simulation>(item.GetData());
            simulation.ETag = item.GetETag();
            simulation.Id = item.GetId();
            return simulation;
        }

        public async Task<Models.Simulation> GetWithStatisticsAsync(string id)
        {
            var simulation = await this.GetAsync(id);

            if (simulation == null) return null;

            simulation.Statistics = await this.simulationStatistics.GetSimulationStatisticsAsync(id);
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
                var connString = await this.connectionStrings.SaveAsync(simulation.IotHubConnectionStrings[index], true);

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
        public async Task<Models.Simulation> UpsertAsync(Models.Simulation simulation, bool validateHubCredentials)
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

            var simulationIsRestarting = false;
            var simulationIsStopping = false;

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

                // Define the user intent looking at the simulation status and the patch content
                simulationIsRestarting = !existingSimulation.Enabled && simulation.Enabled;
                simulationIsStopping = existingSimulation.Enabled && !simulation.Enabled;

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
                var connString = await this.connectionStrings.SaveAsync(simulation.IotHubConnectionStrings[index], validateHubCredentials);

                if (!simulation.IotHubConnectionStrings.Contains(connString))
                {
                    simulation.IotHubConnectionStrings[index] = connString;
                }
            }

            // Note: this code is also in MergeAsync
            // (consider refactoring it out unless the code becomes harder to follow)
            if (simulationIsRestarting)
            {
                simulation = await this.ResetSimulationStatisticsAsync(simulation);
            }
            else if (simulationIsStopping)
            {
                simulation.StoppedTime = DateTimeOffset.UtcNow;

                // When a simulation is disabled, its partitions are deleted.
                // This boolean triggers the deletion of partitions from the storage
                // in the partitioning agent.
                simulation.PartitioningComplete = false;
            }

            return await this.SaveAsync(simulation, simulation.ETag);
        }

        /// <summary>
        /// Modify some simulation details
        /// </summary>
        public async Task<Models.Simulation> MergeAsync(SimulationPatch patch)
        {
            if (string.IsNullOrEmpty(patch.Id))
            {
                this.log.Warn("Invalid simulation ID.", () => new { patch.Id });
                throw new InvalidInputException("Invalid simulation ID.");
            }

            var item = await this.simulationsStorage.GetAsync(patch.Id);
            var simulation = JsonConvert.DeserializeObject<Models.Simulation>(item.GetData());
            simulation.ETag = item.GetETag();
            simulation.Id = item.GetId();

            // Even when there's nothing to do, verify the ETag mismatch
            if (patch.ETag != simulation.ETag)
            {
                this.log.Warn("ETag mismatch",
                    () => new { Current = simulation.ETag, Provided = patch.ETag });
                throw new ConflictingResourceException(
                    $"The ETag provided doesn't match the current resource ETag ({simulation.ETag}).");
            }

            // Define the user intent looking at the simulation status and the patch content
            var simulationIsRestarting = !simulation.Enabled && patch.Enabled.HasValue && patch.Enabled.Value;
            var simulationIsStopping = simulation.Enabled && patch.Enabled.HasValue && !patch.Enabled.Value;

            // Note: this code is also in UpsertAsync
            // (consider refactoring it out unless the code becomes harder to follow)
            if (simulationIsRestarting)
            {
                simulation = await this.ResetSimulationStatisticsAsync(simulation);
            }
            else if (simulationIsStopping)
            {
                simulation.StoppedTime = DateTimeOffset.UtcNow;

                // When a simulation is disabled, its partitions are deleted.
                // This boolean triggers the deletion of partitions from the storage
                // in the partitioning agent.
                simulation.PartitioningComplete = false;

                // Reset active device count to 0
                await this.ResetActiveDevicesStatistics(simulation);
            }

            // The Enabled field is optional, e.g. in case PATCH is extended to
            // modify other fields, so we need to check for null
            if (patch.Enabled != null) simulation.Enabled = patch.Enabled.Value;

            if (patch.DeleteDevicesOnce.HasValue)
            {
                simulation.DeleteDevicesOnce = patch.DeleteDevicesOnce.Value;
            }

            // TODO: can we use this.SaveAsync() here too and avoid the duplication?
            var record = this.simulationsStorage.BuildRecord(simulation.Id, JsonConvert.SerializeObject(simulation));
            item = await this.simulationsStorage.UpsertAsync(record, patch.ETag);

            simulation.ETag = item.GetETag();

            return simulation;
        }

        /// <summary>
        /// Delete a simulation and its statistics (not the devices).
        /// </summary>
        public async Task DeleteAsync(string id)
        {
            await this.simulationStatistics.DeleteSimulationStatisticsAsync(id);
            await this.simulationsStorage.DeleteAsync(id);
        }

        public async Task TrySeedAsync()
        {
            try
            {
                var templateName = this.config.SeedTemplate;

                if (string.IsNullOrEmpty(templateName)) return;

                // Skips if the seeding has already been complete
                if (!await this.mainStorage.ExistsAsync(SEED_STATUS_KEY))
                {
                    await this.SeedSimulationsAsync(templateName);
                    var record = this.mainStorage.BuildRecord(SEED_STATUS_KEY, "Seed Completed");
                    await this.mainStorage.CreateAsync(record);
                }
                else
                {
                    this.log.Info("Seeding skipped.");
                }
            }
            catch (Exception e)
            {
                var msg = "Failed to seed default simulations." + e;
                this.log.Error(msg);
                this.diagnosticsLogger.LogServiceError(msg);
            }
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

                    this.log.Info("Import job created for bulk device creation", () => new { simulationId, simulation.DeviceCreationJobId });

                    await this.SaveAsync(simulation, simulation.ETag);
                }
                catch (Exception e)
                {
                    this.log.Error("Failed to create bulk-device-creation job", e);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Try to create a job to delete all the devices in a simulation.
        /// Throw exception in case of errors.
        ///
        /// This operation is not meant to modify the simulation object, so if the simulation
        /// is still running it will cause errors due to the missing devices. The simulation
        /// might as well recreate the devices if it is running, that is not a problem.
        ///
        /// This method is invoked by the web service, exceptions are thrown to tell the user
        /// what happened. At this point the UI doesn't consume this logic, which is used only
        /// for maintenance.
        /// </summary>
        public async Task<(bool jobCreated, string jobId)> DeleteAllDevicesAsync(string simulationId, IDevices devices)
        {
            try
            {
                var (simulation, jobCreated, jobId) = await this.CreateJobToDeleteDevices(simulationId, devices);
                return (jobCreated, jobId);
            }
            catch (Exception e)
            {
                this.log.Error("Failed to start bulk device deletion", e);
                throw new ExternalDependencyException("Failed to start bulk device deletion", e);
            }
        }

        /// <summary>
        /// Try to create a job to delete all the devices in a simulation.
        /// This method is used by the simulation agent when a simulation ends.
        /// In case of errors the method returns 'false' and the caller is expected to retry.
        /// </summary>
        public async Task<bool> TryToStartDevicesDeletionAsync(string simulationId, IDevices devices)
        {
            try
            {
                var (simulation, jobCreated, jobId) = await this.CreateJobToDeleteDevices(simulationId, devices);

                if (!jobCreated) return true;

                simulation.DeviceDeletionJobId = jobId;
                simulation.DevicesDeletionStarted = true;

                await this.SaveAsync(simulation, simulation.ETag);
                return true;
            }
            catch (Exception e)
            {
                this.log.Error("Failed to start bulk device deletion", e);
                return false;
            }
        }

        public async Task<bool> TryToSetDeviceCreationCompleteAsync(string simulationId)
        {
            var simulation = await this.GetAsync(simulationId);

            // Edit the record only if required
            if (simulation.DevicesCreationComplete) return true;

            simulation.DevicesCreationComplete = true;

            // Reset device deletion state
            simulation.DevicesDeletionComplete = false;
            simulation.DeviceDeletionJobId = null;
            simulation.DevicesDeletionStarted = false;

            return await this.TryToUpdateSimulationAsync(simulation);
        }

        public async Task<bool> TryToSetDeviceDeletionCompleteAsync(string simulationId)
        {
            var simulation = await this.GetAsync(simulationId);

            // Edit the record only if required
            if (simulation.DevicesDeletionComplete) return true;

            simulation.DevicesDeletionComplete = true;

            // Reset device creation state
            simulation.DevicesCreationComplete = false;
            simulation.DeviceCreationJobId = null;
            simulation.DevicesCreationStarted = false;
            simulation.DeleteDevicesOnce = false;

            return await this.TryToUpdateSimulationAsync(simulation);
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
                    deviceIds.Add(this.GenerateId(simulation.Id, model.Id, i));
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

        /// <summary>
        /// Generate a device Id. The method is here to avoid the need to call Dispose
        /// on the registry used by the Devices class.
        /// </summary>
        public string GenerateId(string simulationId, string deviceModelId, int position)
        {
            return simulationId + "." + deviceModelId + "." + position;
        }

        private async Task<(Models.Simulation simulation, bool jobCreated, string jobId)> CreateJobToDeleteDevices(string simulationId, IDevices devices)
        {
            // Fetch latest record, so that updates have the latest ETag if needed
            var simulation = await this.GetAsync(simulationId);

            // If deletion already happened there's nothing to do, return an empty Job ID
            if (simulation.DevicesDeletionStarted || simulation.DevicesDeletionComplete)
            {
                this.log.Info("Bulk device deletion not needed, devices have already been deleted", () => new { simulation.Id });
                return (simulation, false, null);
            }

            Dictionary<string, List<string>> deviceList = this.GetDeviceIdsByModel(simulation);
            var deviceIds = deviceList.SelectMany(x => x.Value).ToList();

            string jobId = await devices.DeleteListUsingJobsAsync(deviceIds);
            this.log.Info("Bulk device deletion started", () => new { simulation.Id, jobId, deviceIds.Count });

            return (simulation, true, jobId);
        }

        private async Task<bool> TryToUpdateSimulationAsync(Models.Simulation simulation)
        {
            try
            {
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

            // When a simulation is disabled, its partitions are deleted - this triggers the deletion
            if (!simulation.Enabled)
            {
                simulation.PartitioningComplete = false;
            }

            IDataRecord record = this.simulationsStorage.BuildRecord(simulation.Id, JsonConvert.SerializeObject(simulation));
            IDataRecord result = await this.simulationsStorage.UpsertAsync(record, eTag);

            // Use the new ETag provided by the storage
            simulation.ETag = result.GetETag();
            simulation.Id = result.GetId();

            this.log.Info("Simulation written to storage",
                () => new
                {
                    simulation.Id,
                    simulation.Enabled,
                    simulation.PartitioningComplete
                });

            return simulation;
        }

        // This creates sample simulations that will be shown on simulation dashboard by default
        private async Task SeedSimulationsAsync(string templateName)
        {
            string content;
            var fileName = templateName + ".json";
            var filePath = Path.Combine(this.config.SeedTemplateFolder, fileName);
            if (this.fileSystem.Exists(filePath))
            {
                content = this.fileSystem.ReadAllText(filePath);
            }
            else
            {
                this.log.Debug("Template not found for setting sample simulations.");
                return;
            }

            Models.Simulation simulation = null;

            try
            {
                var simulationList = JsonConvert.DeserializeObject<List<Models.Simulation>>(content);
                if (simulationList == null || simulationList.Count == 0) return;

                for (int index = 0; index < simulationList.Count; index++)
                {
                    // We need to start creating simulations starting with id 1 as it is treated as default simulation
                    // and is referenced in Welcome page on UI
                    simulation = simulationList[index];
                    var simulationId = index + 1;
                    simulation.Id = simulationId.ToString();

                    try
                    {
                        // Check if there is an existing simulation with the given id
                        // if it exists then skip creating a new simulation
                        await this.GetAsync(simulation.Id);
                    }
                    catch (ResourceNotFoundException)
                    {
                        // create a simulation if no sample simulation exists with provided id.
                        simulation.StartTime = DateTimeOffset.UtcNow;
                        await this.UpsertAsync(simulation, false);
                    }
                }
            }
            catch (Exception ex)
            {
                var message = "Failed to create sample simulation. " + " template: " + JsonConvert.SerializeObject(simulation);
                this.log.Error(message, ex);
            }
        }

        private async Task<Models.Simulation> ResetSimulationStatisticsAsync(Models.Simulation simulation)
        {
            // Reset ActualStartTime, which is used to calculate statistics
            // from the moment when the simulation starts connecting devices and sending telemetry,
            // i.e. after the devices have been created.
            simulation.ActualStartTime = null;

            // Delete statistics records on simulation start
            await this.simulationStatistics.DeleteSimulationStatisticsAsync(simulation.Id);

            return simulation;
        }

        private async Task ResetActiveDevicesStatistics(Models.Simulation simulation)
        {
            try
            {
                var currentStats = await this.simulationStatistics.GetSimulationStatisticsAsync(simulation.Id);

                if (currentStats != null)
                {
                    var simulationModel = new SimulationStatisticsModel
                    {
                        ActiveDevices = 0, // Reset active devices to 0
                        TotalMessagesSent = currentStats.TotalMessagesSent,
                        FailedMessages = currentStats.FailedMessages,
                        FailedDeviceConnections = currentStats.FailedDeviceConnections,
                        FailedDevicePropertiesUpdates = currentStats.FailedDevicePropertiesUpdates,
                    };

                    await this.simulationStatistics.UpdateAsync(simulation.Id, simulationModel);
                }
            }
            catch (Exception e)
            {
                this.log.Error("Error updating active device count statistics.", () => new { simulation.Id, e });
            }
        }
    }
}
