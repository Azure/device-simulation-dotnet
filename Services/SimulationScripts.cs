// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface ISimulationScripts
    {
        /// <summary>
        /// Get list of simulation scripts.
        /// </summary>
        Task<IEnumerable<SimulationScript>> GetListAsync();

        /// <summary>
        /// Get a simulation script.
        /// </summary>
        Task<SimulationScript> GetAsync(string id);

        /// <summary>
        /// Create a simulation script.
        /// </summary>
        Task<SimulationScript> InsertAsync(SimulationScript simulationScript);

        /// <summary>
        /// Create or replace a simulation script.
        /// </summary>
        Task<SimulationScript> UpsertAsync(SimulationScript simulationScript);

        /// <summary>
        /// Delete a simulation script.
        /// </summary>
        Task DeleteAsync(string id);
    }
    public class SimulationScripts : ISimulationScripts
    {
        private const string STORAGE_COLLECTION = "simulationScripts";

        private readonly IStorageAdapterClient storage;
        private readonly ILogger log;

        public SimulationScripts(
            IStorageAdapterClient storage,
            ILogger logger)
        {
            this.storage = storage;
            this.log = logger;
        }

        /// <summary>
        /// Delete a simulation script.
        /// </summary>
        public async Task DeleteAsync(string id)
        {
            try
            {
                await this.storage.DeleteAsync(STORAGE_COLLECTION, id);
            }
            catch (Exception e)
            {
                this.log.Error("Something went wrong while deleting the simulation script.", () => new { id, e });
                throw new ExternalDependencyException("Failed to delete the simulation script", e);
            }
        }

        /// <summary>
        /// Get a simulation script.
        /// </summary>
        public async Task<SimulationScript> GetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                this.log.Error("Simulation script id cannot be empty!", () => { });
                throw new InvalidInputException("Simulation script id cannot be empty! ");
            }

            ValueApiModel item;
            try
            {
                item = await this.storage.GetAsync(STORAGE_COLLECTION, id);
            }
            catch (ResourceNotFoundException)
            {
                throw;
            }
            catch (Exception e)
            {
                this.log.Error("Unable to load simulation script from storage",
                    () => new { id, e.Message, Exception = e });
                throw new ExternalDependencyException("Unable to load simulation script from storage", e);
            }

            try
            {
                var simulationScript = JsonConvert.DeserializeObject<SimulationScript>(item.Data);
                simulationScript.ETag = item.ETag;
                return simulationScript;
            }
            catch (Exception e)
            {
                this.log.Error("Unable to parse simulation script loaded from storage",
                    () => new { id, e.Message, Exception = e });
                throw new ExternalDependencyException("Unable to parse simulation script loaded from storage", e);
            }
        }

        /// <summary>
        /// Get list of simulation scripts.
        /// </summary>
        public async Task<IEnumerable<SimulationScript>> GetListAsync()
        {
            ValueListApiModel data;

            try
            {
                data = await this.storage.GetAllAsync(STORAGE_COLLECTION);
            }
            catch (Exception e)
            {
                this.log.Error("Unable to load simulation scripts from storage",
                    () => new { e.Message, Exception = e });
                throw new ExternalDependencyException("Unable to load simulation scripts from storage", e);
            }

            try
            {
                var results = new List<SimulationScript>();
                foreach (var item in data.Items)
                {
                    var simulationScript = JsonConvert.DeserializeObject<SimulationScript>(item.Data);
                    simulationScript.ETag = item.ETag;
                    simulationScript.Type = ScriptInterpreter.JAVASCRIPT_SCRIPT;
                    simulationScript.Path = SimulationScript.SimulationScriptPath.Storage;
                    results.Add(simulationScript);
                }

                return results;
            }
            catch (Exception e)
            {
                this.log.Error("Unable to parse simulation scripts loaded from storage",
                    () => new { e.Message, Exception = e });
                throw new ExternalDependencyException("Unable to parse simulation scripts loaded from storage", e);
            }
        }

        /// <summary>
        /// Create a simulation script.
        /// </summary>
        public async Task<SimulationScript> InsertAsync(SimulationScript simulationScript)
        {
            simulationScript.Created = DateTimeOffset.UtcNow;
            simulationScript.Modified = simulationScript.Created;
       
            if (string.IsNullOrEmpty(simulationScript.Id))
            {
                simulationScript.Id = Guid.NewGuid().ToString();
            }

            this.log.Debug("Creating a simulation script.", () => new { simulationScript });

            try
            {
                // Note: using UpdateAsync because the service generates the ID
                var result = await this.storage.UpdateAsync(
                    STORAGE_COLLECTION,
                    simulationScript.Id,
                    JsonConvert.SerializeObject(simulationScript),
                    null);

                simulationScript.ETag = result.ETag;
            }
            catch (Exception e)
            {
                this.log.Error("Failed to insert new simulation script into storage",
                    () => new { simulationScript, e });
                throw new ExternalDependencyException(
                    "Failed to insert new simulation script into storage", e);
            }

            return simulationScript;
        }

        /// <summary>
        /// Create or replace a simulation script.
        /// </summary>
        public async Task<SimulationScript> UpsertAsync(SimulationScript simulationScript)
        {
            var id = simulationScript.Id;
            var eTag = simulationScript.ETag;

            try
            {
                var item = await this.GetAsync(id);

                if (item.ETag == eTag)
                {
                    // Replace a custom  simulation script
                    simulationScript.Created = item.Created;
                    simulationScript.Modified = DateTimeOffset.UtcNow;

                    this.log.Debug("Modifying a custom  simulation script via PUT.", () => new { simulationScript });

                    var result = await this.storage.UpdateAsync(
                        STORAGE_COLLECTION,
                        id,
                        JsonConvert.SerializeObject(simulationScript),
                        eTag);

                    // Return the new ETag provided by the storage
                    simulationScript.ETag = result.ETag;
                }
                else
                {
                    this.log.Error("Invalid ETag.", () => new { CurrentETag = item.ETag, ETagProvided = eTag });
                    throw new ConflictingResourceException("Invalid ETag. Simulation script ETag is:'" + item.ETag + "'.");
                }
            }
            catch (ConflictingResourceException)
            {
                throw;
            }
            catch (ResourceNotFoundException)
            {
                this.log.Info("Creating a new  simulation script via PUT", () => new { simulationScript });

                var result = await this.InsertAsync(simulationScript);
                simulationScript.ETag = result.ETag;
            }
            catch (Exception exception)
            {
                this.log.Error("Something went wrong while upserting the  simulation script.", () => new { simulationScript });
                throw new ExternalDependencyException("Failed to upsert: " + exception.Message, exception);
            }

            return simulationScript;
        }
    }
}
