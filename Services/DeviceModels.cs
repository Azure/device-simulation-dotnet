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

// TODO: tests
// TODO: handle errors
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceModels
    {
        IEnumerable<DeviceModel> GetList();
        DeviceModel Get(string id);
        DeviceModel OverrideDeviceModel(DeviceModel source, Models.Simulation.DeviceModelOverride overrideInfo);
    }

    public class DeviceModels : IDeviceModels
    {
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

        public DeviceModel OverrideDeviceModel(DeviceModel source, Models.Simulation.DeviceModelOverride overrideInfo)
        {
            // Generate a clone, so that the original instance remains untouched
            var result = CloneObject(source);

            if (overrideInfo == null) return result;

            this.OverrideDeviceModelSimulationInterval(overrideInfo, result);
            this.OverrideDeviceModelTelemetryCount(overrideInfo, result);
            this.OverrideDeviceModelTelemetryInterval(overrideInfo, result);

            return result;
        }

        // Change the device model simulation details, using the override information
        private void OverrideDeviceModelSimulationInterval(Models.Simulation.DeviceModelOverride overrideInfo, DeviceModel result)
        {
            if (overrideInfo.Simulation?.Interval == null
                || result.Simulation.Interval.ToString("c") == overrideInfo.Simulation.Interval.Value.ToString("c"))
                return;

            this.log.Info("Overriding device state simulation frequency",
                () => new
                {
                    Original = result.Simulation.Interval.ToString("c"),
                    NewValue = overrideInfo.Simulation.Interval.Value.ToString("c")
                });

            result.Simulation.Interval = overrideInfo.Simulation.Interval.Value;
        }

        // Reduce or Increase the number of telemetry messages, if required by the override information
        private void OverrideDeviceModelTelemetryCount(Models.Simulation.DeviceModelOverride overrideInfo, DeviceModel result)
        {
            if (overrideInfo.Telemetry == null || overrideInfo.Telemetry.Count <= 0) return;

            var newCount = overrideInfo.Telemetry.Count;
            var originalCount = result.Telemetry.Count;

            if (originalCount < newCount)
            {
                var diff = newCount - originalCount;
                this.log.Info("The telemetry list is longer than the original model, " +
                              "the extra messages will be added to the model",
                    () => new { originalCount, newCount, diff });
                for (int i = 0; i < diff; i++)
                {
                    result.Telemetry.Add(new DeviceModel.DeviceModelMessage());
                }
            }

            if (originalCount > newCount)
            {
                this.log.Warn("The telemetry list is shorter than the original model, " +
                              "the telemetry messages in excess will be removed from the model",
                    () => new { originalCount, newCount });
                result.Telemetry = result.Telemetry.Take(newCount).ToList();
            }
        }

        // Override the telemetry frequency with the information provided
        private void OverrideDeviceModelTelemetryInterval(Models.Simulation.DeviceModelOverride overrideInfo, DeviceModel result)
        {
            if (overrideInfo.Telemetry == null || overrideInfo.Telemetry.Count <= 0) return;

            for (var i = 0; i < overrideInfo.Telemetry.Count; i++)
            {
                var o = overrideInfo.Telemetry[i];

                if (o.Interval == null
                    || o.Interval.Value.ToString("c") == result.Telemetry[i].Interval.ToString("c"))
                    continue;

                this.log.Info("Changing telemetry frequency",
                    () => new
                    {
                        originalFrequency = result.Telemetry[i].Interval.ToString("c"),
                        newFrequency = o.Interval.Value.ToString("c")
                    });
                result.Telemetry[i].Interval = o.Interval.Value;
            }
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

        // Copy an object by value
        private static T CloneObject<T>(T source)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
        }
    }
}
