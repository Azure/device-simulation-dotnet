// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json.Linq;

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

        /// <summary>
        /// Get property names from all device models.
        /// </summary>
        Task<List<DeviceProperty>> GetPropertyNamesAsync();
    }

    /// <summary>
    /// Proxy to stock and custom device models services.
    /// Note: the exceptions are generated in the stock and custom device
    /// models services and surface as-is without any more catch/wrapping here.
    /// </summary>
    public class DeviceModels : IDeviceModels
    {
        // ID used for custom device models, where the list of sensors is provided by the user
        public const string CUSTOM_DEVICE_MODEL_ID = "custom";

        private readonly ILogger log;
        private readonly ICustomDeviceModels customDeviceModels;
        private readonly IStockDeviceModels stockDeviceModels;

        public DeviceModels(
            ICustomDeviceModels customDeviceModels,
            IStockDeviceModels stockDeviceModels,
            ILogger logger)
        {
            this.log = logger;
            this.stockDeviceModels = stockDeviceModels;
            this.customDeviceModels = customDeviceModels;
        }

        /// <summary>
        /// Get list of device models.
        /// </summary>
        public async Task<IEnumerable<DeviceModel>> GetListAsync()
        {
            var stockDeviceModelsList = this.stockDeviceModels.GetList();
            var customDeviceModelsList = await this.customDeviceModels.GetListAsync();
            var deviceModels = stockDeviceModelsList
                .Concat(customDeviceModelsList)
                .ToList();

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
            if (this.CheckStockDeviceModelExistence(deviceModel.Id))
            {
                throw new ConflictingResourceException(
                    "Device model with id '" + deviceModel.Id + "' already exists!");
            }

            var result = await this.customDeviceModels.InsertAsync(deviceModel);
            deviceModel.ETag = result.ETag;

            return deviceModel;
        }

        /// <summary>
        /// Create or replace a device model.
        /// </summary>
        public async Task<DeviceModel> UpsertAsync(DeviceModel deviceModel)
        {
            if (this.CheckStockDeviceModelExistence(deviceModel.Id))
            {
                this.log.Error("Stock device models cannot be updated",
                    () => new { deviceModel });
                throw new ConflictingResourceException(
                    "Stock device models cannot be updated");
            }

            var result = await this.customDeviceModels.UpsertAsync(deviceModel);
            deviceModel.ETag = result.ETag;

            return deviceModel;
        }

        /// <summary>
        /// Delete a custom device model.
        /// </summary>
        public async Task DeleteAsync(string id)
        {
            if (this.CheckStockDeviceModelExistence(id))
            {
                this.log.Info("Stock device models cannot be deleted",
                    () => new { Id = id });
                throw new UnauthorizedAccessException(
                    "Stock device models cannot be deleted");
            }

            await this.customDeviceModels.DeleteAsync(id);
        }

        /// <summary>
        /// Get property names from all device models.
        /// </summary>
        public async Task<List<DeviceProperty>> GetPropertyNamesAsync()
        {
            /*
             * This will only return the initial device properties that are listed in the Device Model,
             * this will not return all of the properties that are actually associated with the device
             * after start-up.
             * The properties added after startup will come from the new API located in IOT Hub Manager.
             * The UI will merge both these results.
             * */
            var list = await this.GetListAsync();
            var set = new HashSet<string>();

            foreach (var model in list)
            {
                if (model.Properties != null)
                {
                    foreach (var property in model.Properties)
                    {
                        this.PreparePropertyNames(set, property.Value, property.Key);
                    }
                }
            }

            return set.Select(x => new DeviceProperty { Id = x }).ToList();
        }

        /// <summary>
        /// Returns True if there is a stock model with the given Id
        /// </summary>
        private bool CheckStockDeviceModelExistence(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            return this.stockDeviceModels.GetList()
                .Any(model => id.Equals(model.Id, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Transforms property names from Json Object to "xxx.yyy.zzz" format
        /// </summary>
        private void PreparePropertyNames(HashSet<string> set, object obj, string prefix)
        {
            /* Sample conversion:
             * from -> Device : {
             *              Reported : Properties
             *              }
             *          
             * to -> Device.Reported.Properties
             */
            if (obj is JValue)
            {
                set.Add(prefix);
                return;
            }

            if (obj is bool || obj is string || double.TryParse(obj.ToString(), out _))
            {
                set.Add(prefix);
                return;
            }

            foreach (var item in (obj as JToken).Values())
            {
                var path = item.Path;
                this.PreparePropertyNames(set, item, $"{prefix}.{(path.Contains(".") ? path.Substring(path.LastIndexOf('.') + 1) : path)}");
            }
        }
    }
}
