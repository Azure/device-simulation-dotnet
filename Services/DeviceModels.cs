// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;

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

        private List<string> deviceModelFiles;
        private List<DeviceModel> deviceModels;
        private CustomDeviceModels customDeviceModels;
        private StockDeviceModels stockDeviceModels;

        public DeviceModels(
            IStorageAdapterClient storage,
            ICustomDeviceModels CustomDeviceModels,
            IStockDeviceModels StockDeviceModels,
            IServicesConfig config,
            ILogger logger)
        {
            this.storage = storage;
            this.config = config;
            this.log = logger;
            this.stockDeviceModels = new StockDeviceModels(
                this.config,
                this.log);
            this.customDeviceModels = new CustomDeviceModels(
                this.storage,
                this.log);
        }

        /// <summary>
        /// Get list of device models.
        /// </summary>
        public async Task<IEnumerable<DeviceModel>> GetListAsync()
        {
            var deviceModels = new List<DeviceModel>();

            try
            {
                var stockDeviceModels = this.stockDeviceModels.GetList();
                var customDeviceModels = await this.customDeviceModels.GetListAsync();
                deviceModels = stockDeviceModels
                    .Concat(customDeviceModels)
                    .ToList();
            }
            catch (Exception e)
            {
                this.log.Error("Unable to load Device Model ",
                    () => new { e.Message, Exception = e });

                throw new Exception("Unable to load Device Model : " + e.Message, e);
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
            try
            {
                var result = await this.customDeviceModels.UpsertAsync(deviceModel);
                deviceModel.ETag = result.ETag;
            }
            catch(ConflictingResourceException exception)
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
            await this.customDeviceModels.DeleteAsync(id);
        }
    }
}
