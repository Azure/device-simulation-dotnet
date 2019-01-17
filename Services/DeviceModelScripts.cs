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
    public interface IDeviceModelScripts
    {
        /// <summary>
        /// Get list of device model scripts.
        /// </summary>
        Task<IEnumerable<DataFile>> GetListAsync();

        /// <summary>
        /// Get a device model script.
        /// </summary>
        Task<DataFile> GetAsync(string id);

        /// <summary>
        /// Create a device model script.
        /// </summary>
        Task<DataFile> InsertAsync(DataFile deviceModelScript);

        /// <summary>
        /// Create or replace a device model script.
        /// </summary>
        Task<DataFile> UpsertAsync(DataFile deviceModelScript);

        /// <summary>
        /// Delete a device model script.
        /// </summary>
        Task DeleteAsync(string id);
    }

    public class DeviceModelScripts : IDeviceModelScripts
    {
        private const string STORAGE_COLLECTION = "deviceModelScripts";

        private readonly IStorageAdapterClient storage;
        private readonly ILogger log;

        public DeviceModelScripts(
            IStorageAdapterClient storage,
            ILogger logger)
        {
            this.storage = storage;
            this.log = logger;
        }

        /// <summary>
        /// Delete a device model script.
        /// </summary>
        public async Task DeleteAsync(string id)
        {
            try
            {
                await this.storage.DeleteAsync(STORAGE_COLLECTION, id);
            }
            catch (Exception e)
            {
                this.log.Error("Something went wrong while deleting the device model script.", () => new { id, e });
                throw new ExternalDependencyException("Failed to delete the device model script", e);
            }
        }

        /// <summary>
        /// Get a device model script.
        /// </summary>
        public async Task<DataFile> GetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                this.log.Error("Simulation script id cannot be empty!");
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
                this.log.Error("Unable to load device model script from storage", () => new { id, e });
                throw new ExternalDependencyException("Unable to load device model script from storage", e);
            }

            try
            {
                var deviceModelScript = JsonConvert.DeserializeObject<DataFile>(item.Data);
                deviceModelScript.ETag = item.ETag;
                return deviceModelScript;
            }
            catch (Exception e)
            {
                this.log.Error("Unable to parse device model script loaded from storage", () => new { id, e });
                throw new ExternalDependencyException("Unable to parse device model script loaded from storage", e);
            }
        }

        /// <summary>
        /// Get list of device model scripts.
        /// </summary>
        public async Task<IEnumerable<DataFile>> GetListAsync()
        {
            ValueListApiModel data;

            try
            {
                data = await this.storage.GetAllAsync(STORAGE_COLLECTION);
            }
            catch (Exception e)
            {
                this.log.Error("Unable to load device model scripts from storage", e);
                throw new ExternalDependencyException("Unable to load device model scripts from storage", e);
            }

            try
            {
                var results = new List<DataFile>();
                foreach (var item in data.Items)
                {
                    var deviceModelScript = JsonConvert.DeserializeObject<DataFile>(item.Data);
                    deviceModelScript.ETag = item.ETag;
                    deviceModelScript.Type = ScriptInterpreter.JAVASCRIPT_SCRIPT;
                    deviceModelScript.Path = DataFile.FilePath.Storage;
                    results.Add(deviceModelScript);
                }

                return results;
            }
            catch (Exception e)
            {
                this.log.Error("Unable to parse device model scripts loaded from storage", e);
                throw new ExternalDependencyException("Unable to parse device model scripts loaded from storage", e);
            }
        }

        /// <summary>
        /// Create a device model script.
        /// </summary>
        public async Task<DataFile> InsertAsync(DataFile deviceModelScript)
        {
            deviceModelScript.Created = DateTimeOffset.UtcNow;
            deviceModelScript.Modified = deviceModelScript.Created;

            if (string.IsNullOrEmpty(deviceModelScript.Id))
            {
                deviceModelScript.Id = Guid.NewGuid().ToString();
            }

            this.log.Debug("Creating a device model script.", () => new { deviceModelScript });

            try
            {
                // Note: using UpdateAsync because the service generates the ID
                var result = await this.storage.UpdateAsync(
                    STORAGE_COLLECTION,
                    deviceModelScript.Id,
                    JsonConvert.SerializeObject(deviceModelScript),
                    null);

                deviceModelScript.ETag = result.ETag;
            }
            catch (Exception e)
            {
                this.log.Error("Failed to insert new device model script into storage",
                    () => new { deviceModelScript, e });
                throw new ExternalDependencyException(
                    "Failed to insert new device model script into storage", e);
            }

            return deviceModelScript;
        }

        /// <summary>
        /// Create or replace a device model script.
        /// </summary>
        public async Task<DataFile> UpsertAsync(DataFile deviceModelScript)
        {
            var id = deviceModelScript.Id;
            var eTag = deviceModelScript.ETag;

            try
            {
                var item = await this.GetAsync(id);

                if (item.ETag == eTag)
                {
                    // Replace a custom  device model script
                    deviceModelScript.Created = item.Created;
                    deviceModelScript.Modified = DateTimeOffset.UtcNow;

                    this.log.Debug("Modifying a custom device model script.", () => new { deviceModelScript });

                    var result = await this.storage.UpdateAsync(
                        STORAGE_COLLECTION,
                        id,
                        JsonConvert.SerializeObject(deviceModelScript),
                        eTag);

                    // Return the new ETag provided by the storage
                    deviceModelScript.ETag = result.ETag;
                }
                else
                {
                    this.log.Error("Invalid ETag.", () => new { CurrentETag = item.ETag, ETagProvided = eTag });
                    throw new ConflictingResourceException("Invalid ETag. Device model script ETag is:'" + item.ETag + "'.");
                }
            }
            catch (ConflictingResourceException)
            {
                throw;
            }
            catch (ResourceNotFoundException)
            {
                this.log.Info("Creating a new  device model script via PUT", () => new { deviceModelScript });

                var result = await this.InsertAsync(deviceModelScript);
                deviceModelScript.ETag = result.ETag;
            }
            catch (Exception exception)
            {
                this.log.Error("Something went wrong while upserting the device model script.", () => new { deviceModelScript });
                throw new ExternalDependencyException("Failed to upsert: " + exception.Message, exception);
            }

            return deviceModelScript;
        }
    }
}
