// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface ISimulations
    {
        Task<IList<Models.Simulation>> GetListAsync();
        Task<Models.Simulation> GetAsync(string id);
        Task<Models.Simulation> InsertAsync(Models.Simulation simulation, string template = "");
        Task<Models.Simulation> UpsertAsync(Models.Simulation simulation);
        Task<Models.Simulation> MergeAsync(SimulationPatch patch);
        Task DeleteAsync(string id);
    }

    public class Simulations : ISimulations
    {
        private const string STORAGE_COLLECTION = "simulations";
        private const string SIMULATION_ID = "1";
        private const int DEVICES_PER_MODEL_IN_DEFAULT_TEMPLATE = 1;

        private readonly IDeviceModels deviceModels;
        private readonly IStorageAdapterClient storage;
        private readonly ILogger log;

        public Simulations(
            IDeviceModels deviceModels,
            IStorageAdapterClient storage,
            ILogger logger)
        {
            this.deviceModels = deviceModels;
            this.storage = storage;
            this.log = logger;
        }

        public async Task<IList<Models.Simulation>> GetListAsync()
        {
            var data = await this.storage.GetAllAsync(STORAGE_COLLECTION);
            var result = new List<Models.Simulation>();
            foreach (var item in data.Items)
            {
                var simulation = JsonConvert.DeserializeObject<Models.Simulation>(item.Data);
                simulation.Etag = item.ETag;
                result.Add(simulation);
            }

            return result;
        }

        public async Task<Models.Simulation> GetAsync(string id)
        {
            var item = await this.storage.GetAsync(STORAGE_COLLECTION, id);
            var simulation = JsonConvert.DeserializeObject<Models.Simulation>(item.Data);
            simulation.Etag = item.ETag;
            return simulation;
        }

        public async Task<Models.Simulation> InsertAsync(Models.Simulation simulation, string template = "")
        {
            // TODO: complete validation
            if (!string.IsNullOrEmpty(template) && template.ToLowerInvariant() != "default")
            {
                this.log.Warn("Unknown template name", () => new { template });
                throw new InvalidInputException("Unknown template name. Try 'default'.");
            }

            var simulations = await this.GetListAsync();
            if (simulations.Count > 0)
            {
                this.log.Warn("There is already a simulation", () => { });
                throw new ConflictingResourceException(
                    "There is already a simulation. Only one simulation can be created.");
            }

            // Note: forcing the ID because only one simulation can be created
            simulation.Id = SIMULATION_ID;
            simulation.Created = DateTimeOffset.UtcNow;
            simulation.Modified = simulation.Created;
            simulation.Version = 1;

            // Create default simulation
            if (!string.IsNullOrEmpty(template) && template.ToLowerInvariant() == "default")
            {
                var types = this.deviceModels.GetList();
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

            // Note: using UpdateAsync because the service generates the ID
            var result = await this.storage.UpdateAsync(
                STORAGE_COLLECTION,
                SIMULATION_ID,
                JsonConvert.SerializeObject(simulation),
                "*");

            simulation.Etag = result.ETag;

            return simulation;
        }

        /// <summary>
        /// Upsert the simulation. The logic works under the assumption that
        /// there is only one simulation with id "1".
        /// </summary>
        public async Task<Models.Simulation> UpsertAsync(Models.Simulation simulation)
        {
            if (simulation.Id != SIMULATION_ID)
            {
                this.log.Warn("Invalid simulation ID. Only one simulation is allowed", () => { });
                throw new InvalidInputException("Invalid simulation ID. Use ID '" + SIMULATION_ID + "'.");
            }

            var simulations = await this.GetListAsync();
            if (simulations.Count > 0)
            {
                this.log.Info("Modifying simulation via PUT.", () => { });

                if (simulation.Etag != simulations[0].Etag)
                {
                    this.log.Error("Invalid Etag. Running simulation Etag is:'", () => new { simulations });
                    throw new InvalidInputException("Invalid Etag. Running simulation Etag is:'" + simulations[0].Etag + "'.");
                }

                simulation.Created = simulations[0].Created;
                simulation.Modified = DateTimeOffset.UtcNow;
                simulation.Version = simulations[0].Version + 1;
            }
            else
            {
                this.log.Info("Creating new simulation via PUT.", () => { });
                // new simulation
                simulation.Created = DateTimeOffset.UtcNow;
                simulation.Modified = simulation.Created;
                simulation.Version = 1;
            }

            // Note: forcing the ID because only one simulation can be created
            simulation.Id = SIMULATION_ID;
            var item = await this.storage.UpdateAsync(
                STORAGE_COLLECTION,
                SIMULATION_ID,
                JsonConvert.SerializeObject(simulation),
                simulation.Etag);

            // Return the new etag provided by the storage
            simulation.Etag = item.ETag;

            return simulation;
        }

        public async Task<Models.Simulation> MergeAsync(SimulationPatch patch)
        {
            if (patch.Id != SIMULATION_ID)
            {
                this.log.Warn("Invalid simulation ID.", () => new { patch.Id });
                throw new InvalidInputException("Invalid simulation ID. Use ID '" + SIMULATION_ID + "'.");
            }

            var item = await this.storage.GetAsync(STORAGE_COLLECTION, patch.Id);
            var simulation = JsonConvert.DeserializeObject<Models.Simulation>(item.Data);
            simulation.Etag = item.ETag;

            // Even when there's nothing to do, verify the etag mismatch
            if (patch.Etag != simulation.Etag)
            {
                this.log.Warn("Etag mismatch",
                    () => new { Current = simulation.Etag, Provided = patch.Etag });
                throw new ConflictingResourceException(
                    $"The ETag provided doesn't match the current resource ETag ({simulation.Etag}).");
            }

            if (!patch.Enabled.HasValue || patch.Enabled.Value == simulation.Enabled)
            {
                // Nothing to do
                return simulation;
            }

            simulation.Enabled = patch.Enabled.Value;
            simulation.Modified = DateTimeOffset.UtcNow;
            simulation.Version += 1;

            item = await this.storage.UpdateAsync(
                STORAGE_COLLECTION,
                SIMULATION_ID,
                JsonConvert.SerializeObject(simulation),
                patch.Etag);

            simulation.Etag = item.ETag;

            return simulation;
        }

        public async Task DeleteAsync(string id)
        {
            await this.storage.DeleteAsync(STORAGE_COLLECTION, id);
        }
    }
}
