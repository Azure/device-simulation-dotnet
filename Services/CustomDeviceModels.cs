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
    public interface ICustomDeviceModels
    {
        /// <summary>
        /// Get list of custom device models.
        /// </summary>
        Task<IEnumerable<DeviceModel>> GetListAsync();

        /// <summary>
        /// Get a custom device model.
        /// </summary>
        Task<DeviceModel> GetAsync(string id);

        /// <summary>
        /// Create a custom device model.
        /// </summary>
        Task<DeviceModel> InsertAsync(DeviceModel deviceModel, bool generateId = true);

        /// <summary>
        /// Create or replace a custom device model.
        /// </summary>
        Task<DeviceModel> UpsertAsync(DeviceModel deviceModel);

        /// <summary>
        /// Delete a custom device model.
        /// </summary>
        Task DeleteAsync(string id);
    }

    public class CustomDeviceModels : ICustomDeviceModels
    {
        private const string STORAGE_COLLECTION = "deviceModels";

        private readonly IStorageAdapterClient storage;
        private readonly ILogger log;

        public CustomDeviceModels(
            IStorageAdapterClient storage,
            ILogger logger)
        {
            this.storage = storage;
            this.log = logger;
        }

        /// <summary>
        /// Get list of custom device models.
        /// </summary>
        public async Task<IEnumerable<DeviceModel>> GetListAsync()
        {
            ValueListApiModel data;

            try
            {
                data = await this.storage.GetAllAsync(STORAGE_COLLECTION);
            }
            catch (Exception e)
            {
                this.log.Error("Unable to load device models from storage",
                    () => new { e.Message, Exception = e });
                throw new ExternalDependencyException("Unable to load device models from storage", e);
            }

            try
            {
                var results = new List<DeviceModel>();
                foreach (var item in data.Items)
                {
                    var deviceModel = JsonConvert.DeserializeObject<DeviceModel>(item.Data);
                    deviceModel.ETag = item.ETag;
                    deviceModel.Type = DeviceModel.DeviceModelType.Custom;
                    results.Add(deviceModel);
                }

                return results;
            }
            catch (Exception e)
            {
                this.log.Error("Unable to parse device models loaded from storage",
                    () => new { e.Message, Exception = e });
                throw new ExternalDependencyException("Unable to parse device models loaded from storage", e);
            }
        }

        /// <summary>
        /// Get a custom device model.
        /// </summary>
        public async Task<DeviceModel> GetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                this.log.Error("Device model id cannot be empty!", () => { });
                throw new InvalidInputException("Device model id cannot be empty! ");
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
                this.log.Error("Unable to load device model from storage",
                    () => new { id, e.Message, Exception = e });
                throw new ExternalDependencyException("Unable to load device model from storage", e);
            }

            try
            {
                var deviceModel = JsonConvert.DeserializeObject<DeviceModel>(item.Data);
                deviceModel.ETag = item.ETag;
                return deviceModel;
            }
            catch (Exception e)
            {
                this.log.Error("Unable to parse device model loaded from storage",
                    () => new { id, e.Message, Exception = e });
                throw new ExternalDependencyException("Unable to parse device model loaded from storage", e);
            }
        }

        /// <summary>
        /// Create a custom device model.
        /// </summary>
        public async Task<DeviceModel> InsertAsync(DeviceModel deviceModel, bool generateId = true)
        {
            deviceModel.Created = DateTimeOffset.UtcNow;
            deviceModel.Modified = deviceModel.Created;
            deviceModel.Type = DeviceModel.DeviceModelType.Custom;

            if (generateId)
            {
                deviceModel.Id = Guid.NewGuid().ToString();
            }

            this.log.Debug("Creating a custom device model.", () => new { deviceModel });

            try
            {
                // Note: using UpdateAsync because the service generates the ID
                var result = await this.storage.UpdateAsync(
                    STORAGE_COLLECTION,
                    deviceModel.Id,
                    JsonConvert.SerializeObject(deviceModel),
                    null);

                deviceModel.ETag = result.ETag;
            }
            catch (Exception e)
            {
                this.log.Error("Failed to insert new device model into storage",
                    () => new { deviceModel, generateId, e });
                throw new ExternalDependencyException(
                    "Failed to insert new device model into storage", e);
            }

            return deviceModel;
        }

        /// <summary>
        /// Create or replace a custom device model.
        /// </summary>
        public async Task<DeviceModel> UpsertAsync(DeviceModel deviceModel)
        {
            var id = deviceModel.Id;
            var eTag = deviceModel.ETag;

            try
            {
                var item = await this.GetAsync(id);

                if (item.ETag == eTag)
                {
                    // Replace a custom device model
                    deviceModel.Created = item.Created;
                    deviceModel.Modified = DateTimeOffset.UtcNow;

                    this.log.Debug("Modifying a custom device model via PUT.", () => new { deviceModel });

                    var result = await this.storage.UpdateAsync(
                        STORAGE_COLLECTION,
                        id,
                        JsonConvert.SerializeObject(deviceModel),
                        eTag);

                    // Return the new ETag provided by the storage
                    deviceModel.ETag = result.ETag;
                }
                else
                {
                    this.log.Error("Invalid ETag.", () => new { CurrentETag = item.ETag, ETagProvided = eTag });
                    throw new ConflictingResourceException("Invalid ETag. Device Model ETag is:'" + item.ETag + "'.");
                }
            }
            catch (ConflictingResourceException)
            {
                throw;
            }
            catch (ResourceNotFoundException)
            {
                this.log.Info("Creating a new device model via PUT", () => new { deviceModel });

                var result = await this.InsertAsync(deviceModel, false);
                deviceModel.ETag = result.ETag;
            }
            catch (Exception exception)
            {
                this.log.Error("Something went wrong while upserting the device model.", () => new { deviceModel });
                throw new ExternalDependencyException("Failed to upsert: " + exception.Message, exception);
            }

            return deviceModel;
        }

        /// <summary>
        /// Delete a custom device model.
        /// </summary>
        public async Task DeleteAsync(string id)
        {
            try
            {
                await this.storage.DeleteAsync(STORAGE_COLLECTION, id);
            }
            catch (Exception e)
            {
                this.log.Error("Something went wrong while deleting the device model.", () => new { id, e });
                throw new ExternalDependencyException("Failed to delete the device model", e);
            }
        }
    }
}
