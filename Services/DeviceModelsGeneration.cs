// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceModelsGeneration
    {
        DeviceModel Generate(DeviceModel source, Models.Simulation.DeviceModelOverride overrideInfo);
    }

    public class DeviceModelsGeneration : IDeviceModelsGeneration
    {
        private readonly ILogger log;

        public DeviceModelsGeneration(ILogger logger)
        {
            this.log = logger;
        }

        // Change the device model simulation details, using the override information
        public DeviceModel Generate(DeviceModel source, Models.Simulation.DeviceModelOverride overrideInfo)
        {
            // Generate a clone, so that the original instance remains untouched
            var result = CloneObject(source);

            if (overrideInfo == null) return result;

            this.UpdateDeviceModelSimulationScriptsCount(overrideInfo.Simulation?.Scripts, result);
            this.UpdateDeviceModelTelemetryCount(overrideInfo.Telemetry, result);

            this.SetSimulationInitialState(overrideInfo.Simulation?.InitialState, result);
            this.SetSimulationInterval(overrideInfo.Simulation?.Interval, result);
            this.SetSimulationScripts(overrideInfo.Simulation?.Scripts, result);
            this.SetTelemetry(overrideInfo.Telemetry, result);

            return result;
        }

        private void SetSimulationInitialState(Dictionary<string, object> state, DeviceModel result)
        {
            if (state == null) return;

            this.log.Debug("Overriding initial state of the device", () => new
            {
                Original = result.Simulation.InitialState,
                NewValue = state
            });

            result.Simulation.InitialState = state;
        }

        // Redefine the interval at which the device state is generated
        private void SetSimulationInterval(TimeSpan? interval, DeviceModel result)
        {
            if (interval == null || result.Simulation.Interval.ToString("c") == interval.Value.ToString("c"))
                return;

            this.log.Debug("Overriding device state simulation frequency",
                () => new
                {
                    Original = result.Simulation.Interval.ToString("c"),
                    NewValue = interval.Value.ToString("c")
                });

            result.Simulation.Interval = interval.Value;
        }

        // Reduce or Increase the number of scripts, if required by the override information
        private void UpdateDeviceModelSimulationScriptsCount(
            IList<Models.Simulation.DeviceModelSimulationScriptOverride> scripts, DeviceModel result)
        {
            if (scripts == null || scripts.Count == 0) return;

            var newCount = scripts.Count;
            var originalCount = result.Simulation.Scripts.Count;

            if (originalCount < newCount)
            {
                this.log.Debug("The list of scripts is longer than the original model, " +
                              "the extra scripts will be added to the model",
                    () => new { originalCount, newCount });

                for (int i = 0; i < newCount - originalCount; i++)
                {
                    result.Simulation.Scripts.Add(new Script());
                }
            }

            if (originalCount > newCount)
            {
                this.log.Warn("The list of scripts is shorter than the original model, " +
                              "the scripts in excess will be removed from the model",
                    () => new { originalCount, newCount });

                result.Simulation.Scripts = result.Simulation.Scripts.Take(newCount).ToList();
            }
        }

        // Reduce or Increase the number of telemetry messages, if required by the override information
        private void UpdateDeviceModelTelemetryCount(
            IList<Models.Simulation.DeviceModelTelemetryOverride> telemetry, DeviceModel result)
        {
            if (telemetry == null
                || telemetry.Count == 0
                || telemetry.Count == result.Telemetry.Count) return;

            var newCount = telemetry.Count;
            var originalCount = result.Telemetry.Count;

            this.log.Debug("The length of the telemetry list is different from the original model, adding/removing the extra telemetry",
                () => new { originalCount, newCount });

            if (originalCount < newCount)
            {
                // Add the missing elements (empty for now)
                for (int i = 0; i < newCount - originalCount; i++)
                {
                    result.Telemetry.Add(new DeviceModel.DeviceModelMessage());
                }
            }
            else
            {
                // Remove what's not used
                result.Telemetry = result.Telemetry.Take(newCount).ToList();
            }
        }

        // Redefine the scripts used to generate the device state
        private void SetSimulationScripts(
            IList<Models.Simulation.DeviceModelSimulationScriptOverride> scripts, DeviceModel result)
        {
            if (scripts == null || scripts.Count == 0) return;

            this.log.Debug("Overriding device state simulation scripts",
                () => new
                {
                    Original = result.Simulation.Scripts,
                    NewValue = scripts
                });

            for (var i = 0; i < scripts.Count; i++)
            {
                var script = scripts[i];
                result.Simulation.Scripts[i].Params = script.Params;

                if (!string.IsNullOrEmpty(script.Type))
                {
                    result.Simulation.Scripts[i].Type = script.Type;
                }

                if (!string.IsNullOrEmpty(script.Path))
                {
                    result.Simulation.Scripts[i].Path = script.Path;
                }
            }
        }

        // Override the telemetry frequency and content with the information provided
        private void SetTelemetry(
            IList<Models.Simulation.DeviceModelTelemetryOverride> telemetry, DeviceModel result)
        {
            if (telemetry == null || telemetry.Count == 0) return;

            for (var i = 0; i < telemetry.Count; i++)
            {
                var t = telemetry[i];

                if (t.Interval != null
                    && t.Interval.Value.ToString("c") != result.Telemetry[i].Interval.ToString("c"))
                {
                    this.log.Debug("Changing telemetry frequency",
                        () => new
                        {
                            originalFrequency = result.Telemetry[i].Interval.ToString("c"),
                            newFrequency = t.Interval.Value.ToString("c")
                        });

                    result.Telemetry[i].Interval = t.Interval.Value;
                }

                if (!string.IsNullOrEmpty(t.MessageTemplate)
                    && t.MessageTemplate != result.Telemetry[i].MessageTemplate)
                {
                    this.log.Debug("Changing telemetry message template",
                        () => new
                        {
                            originalTemplate = result.Telemetry[i].MessageTemplate,
                            newTemplate = t.MessageTemplate
                        });

                    result.Telemetry[i].MessageTemplate = t.MessageTemplate;
                }

                if (t.MessageSchema != null)
                {
                    if (!string.IsNullOrEmpty(t.MessageSchema.Name)
                        && t.MessageSchema.Name != result.Telemetry[i].MessageSchema.Name)
                    {
                        this.log.Debug("Changing telemetry message schema name",
                            () => new
                            {
                                originalName = result.Telemetry[i].MessageSchema.Name,
                                newName = t.MessageSchema.Name
                            });

                        result.Telemetry[i].MessageSchema.Name = t.MessageSchema.Name;
                    }

                    if (t.MessageSchema.Format != null
                        && t.MessageSchema.Format != result.Telemetry[i].MessageSchema.Format)
                    {
                        this.log.Debug("Changing telemetry message schema format",
                            () => new
                            {
                                originalFormat = result.Telemetry[i].MessageSchema.Format,
                                newFormat = t.MessageSchema.Format
                            });

                        result.Telemetry[i].MessageSchema.Format = t.MessageSchema.Format.Value;
                    }

                    if (t.MessageSchema.Fields != null)
                    {
                        this.log.Debug("Changing telemetry message schema fields",
                            () => new
                            {
                                originalFields = result.Telemetry[i].MessageSchema.Fields,
                                newFields = t.MessageSchema.Fields
                            });
                        result.Telemetry[i].MessageSchema.Fields = t.MessageSchema.Fields;
                    }
                }
            }
        }

        // Copy an object by value
        private static T CloneObject<T>(T source)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
        }
    }
}
