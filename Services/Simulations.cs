// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
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
    }

    public class Simulations : ISimulations
    {
        private const string StorageCollection = "simulations";
        private const string SimulationId = "1";
        private const int DevicesPerTypeInDefaultTemplate = 2;

        private readonly IDeviceTypes deviceTypes;
        private readonly IStorageAdapterClient storage;
        private readonly ILogger log;

        public Simulations(
            IDeviceTypes deviceTypes,
            IStorageAdapterClient storage,
            ILogger logger)
        {
            this.deviceTypes = deviceTypes;
            this.storage = storage;
            this.log = logger;
        }

        public async Task<IList<Models.Simulation>> GetListAsync()
        {
            var data = await this.storage.GetAllAsync(StorageCollection);
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
            var item = await this.storage.GetAsync(StorageCollection, id);
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
            simulation.Id = SimulationId;
            simulation.Created = DateTimeOffset.UtcNow;
            simulation.Modified = simulation.Created;
            simulation.Version = 1;

            // Create default simulation
            if (!string.IsNullOrEmpty(template) && template.ToLowerInvariant() == "default")
            {
                var types = this.deviceTypes.GetList();
                simulation.DeviceTypes = new List<Models.Simulation.DeviceTypeRef>();
                foreach (var type in types)
                {
                    simulation.DeviceTypes.Add(new Models.Simulation.DeviceTypeRef
                    {
                        Id = type.Id,
                        Count = DevicesPerTypeInDefaultTemplate
                    });
                }
            }

            // Note: using UpdateAsync because the service generates the ID
            await this.storage.UpdateAsync(
                StorageCollection,
                SimulationId,
                JsonConvert.SerializeObject(simulation),
                "*");

            return simulation;
        }

        /// <summary>
        /// Upsert the simulation. The logic works under the assumption that
        /// there is only one simulation with id "1".
        /// </summary>
        public async Task<Models.Simulation> UpsertAsync(Models.Simulation simulation)
        {
            if (simulation.Id != SimulationId)
            {
                this.log.Warn("Invalid simulation ID. Only one simulation is allowed", () => { });
                throw new InvalidInputException("Invalid simulation ID. Use ID '" + SimulationId + "'.");
            }

            var simulations = await this.GetListAsync();
            if (simulations.Count > 0)
            {
                //simulation.Modified = DateTimeOffset.UtcNow;
                //simulation.Version = simulations.First().Version + 1;
                this.log.Error("Simulations cannot be modified via PUT. Use PATCH to start/stop the simulation.", () => { });
                throw new InvalidInputException("Simulations cannot be modified via PUT. Use PATCH to start/stop the simulation.");
            }

            // Note: forcing the ID because only one simulation can be created
            simulation.Id = SimulationId;
            simulation.Created = DateTimeOffset.UtcNow;
            simulation.Modified = simulation.Created;
            simulation.Version = 1;

            await this.storage.UpdateAsync(
                StorageCollection,
                SimulationId,
                JsonConvert.SerializeObject(simulation),
                simulation.Etag);

            return simulation;
        }

        public async Task<Models.Simulation> MergeAsync(SimulationPatch patch)
        {
            if (patch.Id != SimulationId)
            {
                this.log.Warn("Invalid simulation ID.", () => new { patch.Id });
                throw new InvalidInputException("Invalid simulation ID. Use ID '" + SimulationId + "'.");
            }

            var item = await this.storage.GetAsync(StorageCollection, patch.Id);
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
                StorageCollection,
                SimulationId,
                JsonConvert.SerializeObject(simulation),
                patch.Etag);

            simulation.Etag = item.ETag;

            return simulation;
        }
    }
}
