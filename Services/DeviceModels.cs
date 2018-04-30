// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

// TODO: tests
// TODO: handle errors
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceModels
    {
        /// <summary>
        /// Get list of device models.
        /// </summary>
        Task<IEnumerable<DeviceModel>> GetListAsync();

        /// <summary>
        /// Get a device model.
        /// </summary>
        Task<DeviceModel> GetAsync(string id);

        /// <summary>
        /// Create a device model.
        /// </summary>
        Task<DeviceModel> InsertAsync(DeviceModel deviceModel);

        /// <summary>
        /// Create or replace a device model.
        /// </summary>
        Task<DeviceModel> UpsertAsync(DeviceModel deviceModel);

        /// <summary>
        /// Delete a custom device model.
        /// </summary>
        Task DeleteAsync(string id);
    }

    public class DeviceModels : IDeviceModels
    {
        // ID used for custom device models, where the list of sensors is provided by the user
        public const string CUSTOM_DEVICE_MODEL_ID = "custom";

        private const string EXT = ".json";

        private readonly IServicesConfig config;
        private readonly ILogger log;
        private readonly IStorageAdapterClient storage;
        
        private readonly ICustomDeviceModels customDeviceModels;
        private readonly IStockDeviceModels stockDeviceModels;

        public DeviceModels(
            IStorageAdapterClient storage,
            ICustomDeviceModels customDeviceModels,
            IStockDeviceModels stockDeviceModels,
            IServicesConfig config,
            ILogger logger)
        {
            this.storage = storage;
            this.config = config;
            this.log = logger;
            this.stockDeviceModels = stockDeviceModels;
            this.customDeviceModels = customDeviceModels;
        }

        /// <summary>
        /// Get list of device models.
        /// </summary>
        public async Task<IEnumerable<DeviceModel>> GetListAsync()
        {
            List<DeviceModel> deviceModels;

            try
            {
                var stockDeviceModelsList = this.stockDeviceModels.GetList();
                var customDeviceModelsList = await this.customDeviceModels.GetListAsync();
                deviceModels = stockDeviceModelsList
                    .Concat(customDeviceModelsList)
                    .ToList();
            }
            catch (Exception e)
            {
                this.log.Error("Unable to load Device Models ",
                    () => new { e.Message, Exception = e });

                throw new ExternalDependencyException("Unable to load Device Models : ", e);
            }

            return deviceModels;
        }

        /// <summary>
        /// Get a device model.
        /// </summary>
        public async Task<DeviceModel> GetAsync(string id)
        {
            var list = await this.GetListAsync();
            var item = list.FirstOrDefault(i => i.Id == id);
            if (item != null)
                return item;

            this.log.Warn("Device model not found", () => new { id });

            throw new ResourceNotFoundException();
        }

        /// <summary>
        /// Create a device model.
        /// </summary>
        public async Task<DeviceModel> InsertAsync(DeviceModel deviceModel)
        {
            if (this.CheckDeviceModelExistence(deviceModel.Id))
            {
                throw new ConflictingResourceException("Device model with id '" + deviceModel.Id + "'already existed!");
            }

            try
            {
                var result = await this.customDeviceModels.InsertAsync(deviceModel);
                deviceModel.ETag = result.ETag;
            }
            catch (Exception e)
            {
                this.log.Error("Unable to create a new device model", () => new { Exception = e });
            }

            return deviceModel;
        }

        /// <summary>
        /// Create or replace a device model.
        /// </summary>
        public async Task<DeviceModel> UpsertAsync(DeviceModel deviceModel)
        {
            if (this.CheckDeviceModelExistence(deviceModel.Id))
            {
                throw new ConflictingResourceException("Device model with id '" + deviceModel.Id + "'already existed!");
            }

            try
            {
                var result = await this.customDeviceModels.UpsertAsync(deviceModel);
                deviceModel.ETag = result.ETag;
            }
            catch (ConflictingResourceException exception)
            {
                this.log.Error("Unable to update deivce model :'", () => new { exception });
            }
            catch (Exception exception)
            {
                this.log.Error("Unable to update deivce model :'", () => new { exception });
            }

            return deviceModel;
        }

        /// <summary>
        /// Delete a custom device model.
        /// </summary>
        public async Task DeleteAsync(string id)
        {
            if (this.CheckDeviceModelExistence(id))
            {
                throw new UnauthorizedAccessException("Cannot delete a stock device model");
            }

            await this.customDeviceModels.DeleteAsync(id);
        }

        private bool CheckDeviceModelExistence(string id)
        {
            return  this.stockDeviceModels.GetList().Any(model => model.Id == id);
        }
    }
}
