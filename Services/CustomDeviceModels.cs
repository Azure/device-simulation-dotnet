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
        Task<DeviceModel> InsertAsync(DeviceModel deviceModel);

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
            var data = await this.storage.GetAllAsync(STORAGE_COLLECTION);
            var results = new List<DeviceModel>();
            foreach (var item in data.Items)
            {
                var deviceModel = JsonConvert.DeserializeObject<DeviceModel>(item.Data);
                deviceModel.ETag = item.ETag;
                deviceModel.Type = "CustomModel";
                results.Add(deviceModel);
            }

            return results;
        }

        /// <summary>
        /// Get a custom device model.
        /// </summary>
        public async Task<DeviceModel> GetAsync(string id)
        {
            var item = await this.storage.GetAsync(STORAGE_COLLECTION, id);
            var deviceModel = JsonConvert.DeserializeObject<DeviceModel>(item.Data);
            deviceModel.ETag = item.ETag;
            return deviceModel;
        }

        /// <summary>
        /// Create a custom device model.
        /// </summary>
        public async Task<DeviceModel> InsertAsync(DeviceModel deviceModel)
        {
            deviceModel.Created = DateTimeOffset.UtcNow;
            deviceModel.Modified = deviceModel.Created;
            deviceModel.Id = Guid.NewGuid().ToString();

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
            var Id = deviceModel.Id;
            var ETag = deviceModel.ETag;

            try
            {
                var item = await this.GetAsync(Id);

                if (item.ETag == ETag)
                {
                    // Replace a custom device model
                    deviceModel.Created = item.Created;
                    deviceModel.Modified = DateTimeOffset.UtcNow;
                    
                    var result = await this.storage.UpdateAsync(
                        STORAGE_COLLECTION,
                        Id,
                        JsonConvert.SerializeObject(deviceModel),
                        ETag);

                    // Return the new ETag provided by the storage
                    deviceModel.ETag = result.ETag;
                }
                else
                {
                    this.log.Error("Invalid ETag. Current Device Model ETag is:'", () => new { ETag = item.ETag });
                    throw new ConflictingResourceException("Invalid ETag. Running simulation ETag is:'" + item.ETag + "'.");
                }
            }
            catch (ResourceNotFoundException)
            {
                this.log.Info("Creating new device model via PUT.", () => { });

                var result = await this.InsertAsync(deviceModel);
                deviceModel.ETag = result.ETag;
            }
            catch(Exception exception)
            {
                this.log.Error("Something went wrong when modify the device model.", () => new { exception });
                throw exception;
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
