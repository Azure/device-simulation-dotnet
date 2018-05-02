// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        private const string CUSTOMMODEL = "CustomModel";

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
            var data = await this.storage.GetAllAsync(STORAGE_COLLECTION);
            var results = new List<DeviceModel>();
            foreach (var item in data.Items)
            {
                var deviceModel = JsonConvert.DeserializeObject<DeviceModel>(item.Data);
                deviceModel.ETag = item.ETag;
                deviceModel.Type = CUSTOMMODEL;
                results.Add(deviceModel);
            }

            return results;
        }

        /// <summary>
        /// Get a custom device model.
        /// </summary>
        public async Task<DeviceModel> GetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                this.log.Error("Device model id cannot be empty!", () => {});
                throw new InvalidInputException("Device model id cannot be empty! ");
            }

            var item = await this.storage.GetAsync(STORAGE_COLLECTION, id);
            var deviceModel = JsonConvert.DeserializeObject<DeviceModel>(item.Data);
            deviceModel.ETag = item.ETag;
            return deviceModel;
        }

        /// <summary>
        /// Create a custom device model.
        /// </summary>
        public async Task<DeviceModel> InsertAsync(DeviceModel deviceModel, bool generateId = true)
        {
            deviceModel.Created = DateTimeOffset.UtcNow;
            deviceModel.Modified = deviceModel.Created;

            if (generateId)
            {
                deviceModel.Id = Guid.NewGuid().ToString();
            }

            this.log.Debug("Create custom device model: ", () => new { deviceModel });

            // Note: using UpdateAsync because the service generates the ID
            var result = await this.storage.UpdateAsync(
                STORAGE_COLLECTION,
                deviceModel.Id,
                JsonConvert.SerializeObject(deviceModel),
                null);

            deviceModel.ETag = result.ETag;

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

                    this.log.Debug("Modify a custom device model: ", () => new { deviceModel });

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
                    this.log.Error("Invalid ETag'", () => new { CurrentETag = item.ETag, ETagProvided = eTag });
                    throw new ConflictingResourceException("Invalid ETag. Device Model ETag is:'" + item.ETag + "'.");
                }
            }
            catch (ResourceNotFoundException)
            {
                this.log.Info("Creating new device model via PUT.", () => new { deviceModel });

                var result = await this.InsertAsync(deviceModel, false);
                deviceModel.ETag = result.ETag;
            }
            catch (Exception exception)
            {
                this.log.Error("Something went wrong while upserting the device model.", () => new { exception });
            }

            return deviceModel;
        }

        /// <summary>
        /// Delete a custom device model.
        /// </summary>
        public async Task DeleteAsync(string id)
        {
            await this.storage.DeleteAsync(STORAGE_COLLECTION, id);
        }
    }
}
