// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// TODO: tests
// TODO: handle errors
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceModels
    {
        IEnumerable<DeviceModel> GetList();
        DeviceModel Get(string id);
        HashSet<string> GetPropNames();
    }

    public class DeviceModels : IDeviceModels
    {
        // ID used for custom device models, where the list of sensors is provided by the user
        public const string CUSTOM_DEVICE_MODEL_ID = "custom";

        private const string EXT = ".json";

        private readonly IServicesConfig config;
        private readonly ILogger log;

        private List<string> deviceModelFiles;
        private List<DeviceModel> deviceModels;

        public DeviceModels(
            IServicesConfig config,
            ILogger logger)
        {
            this.config = config;
            this.log = logger;
            this.deviceModelFiles = null;
            this.deviceModels = null;
        }

        public IEnumerable<DeviceModel> GetList()
        {
            if (this.deviceModels != null) return this.deviceModels;

            this.deviceModels = new List<DeviceModel>();

            try
            {
                var files = this.GetDeviceModelFiles();
                foreach (var f in files)
                {
                    var c = JsonConvert.DeserializeObject<DeviceModel>(File.ReadAllText(f));
                    this.deviceModels.Add(c);
                }
            }
            catch (Exception e)
            {
                this.log.Error("Unable to load Device Model configuration",
                    () => new { e.Message, Exception = e });

                throw new InvalidConfigurationException("Unable to load Device Model configuration: " + e.Message, e);
            }

            return this.deviceModels;
        }

        public DeviceModel Get(string id)
        {
            var list = this.GetList();
            var item = list.FirstOrDefault(i => i.Id == id);
            if (item != null)
                return item;

            this.log.Warn("Device model not found", () => new { id });

            throw new ResourceNotFoundException();
        }

        private List<string> GetDeviceModelFiles()
        {
            if (this.deviceModelFiles != null) return this.deviceModelFiles;

            this.log.Debug("Device models folder", () => new { this.config.DeviceModelsFolder });

            var fileEntries = Directory.GetFiles(this.config.DeviceModelsFolder);

            this.deviceModelFiles = fileEntries.Where(fileName => fileName.EndsWith(EXT)).ToList();

            this.log.Debug("Device model files", () => new { this.deviceModelFiles });

            return this.deviceModelFiles;
        }

        public HashSet<string> GetPropNames()
        {
            var list = this.GetList().ToList();
            if (list.Count > 0)
            {
                var set = new HashSet<string>();
                /* The properties from this model is only intended to provide a default set when the application loads for the first time.
                 The properties added after startup will come from the new API located in IOT Hub Manager. The UI will merge both the results. */
                list.ForEach(m =>
                {
                    foreach (var item in m.Properties)
                    {
                        this.PreparePropNames(set, item.Value, item.Key);
                    }
                });
                return set;
            }

            return null;
        }

        private void PreparePropNames(HashSet<string> set, object obj, string prefix)
        {
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
                /* Convert
                 * Device : {
                 *          Reported : Properties
                 *          }
                 *          
                 * to Device.Reported.Properties and keep adding to hashset
                 */
                this.PreparePropNames(set, item, $"{prefix}.{(path.Contains(".") ? path.Substring(path.LastIndexOf('.') + 1) : path)}");
            }
        }
    }
}
