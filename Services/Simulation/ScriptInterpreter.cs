// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation
{
    public interface IScriptInterpreter
    {
        Dictionary<string, object> Invoke(
            Script script,
            Dictionary<string, object> context,
            Dictionary<string, object> state);
    }

    public class ScriptInterpreter : IScriptInterpreter
    {
        public const string SENSOR_SIMULATION_ENABLED = "SENSOR_SIMULATION_ENABLED";
        public const string TELEMETRY_ENABLED = "TELEMETRY_ENABLED";
        public const string DEVICE_METHOD_STATUS = "DeviceMethodStatus";

        private readonly IJavascriptInterpreter jsInterpreter;
        private readonly ILogger log;

        public ScriptInterpreter(
            IJavascriptInterpreter jsInterpreter,
            ILogger logger)
        {
            this.jsInterpreter = jsInterpreter;
            this.log = logger;
        }

        public Dictionary<string, object> Invoke(
            Script script,
            Dictionary<string, object> context,
            Dictionary<string, object> state)
        {
            switch (script.Type.ToLowerInvariant())
            {
                default:
                    this.log.Error("Unknown script type", () => new { script.Type });
                    throw new NotSupportedException($"Unknown script type `${script.Type}`.");

                case "javascript":
                    this.log.Debug("Invoking JS", () => new { script.Path, context, state });
                    var result = this.jsInterpreter.Invoke(script.Path, context, state);
                    this.log.Debug("JS result", () => new { result });
                    return result;
            }
        }

        public static void AddMethodStatusProperty(Dictionary<string, object> state)
        {
            state[DEVICE_METHOD_STATUS] = "";
        }

        public static void EnableSensorSimulation(Dictionary<string, object> state)
        {
            state.Add(SENSOR_SIMULATION_ENABLED, true);
        }

        public static void EnableTelemetry(Dictionary<string, object> state)
        {
            state.Add(TELEMETRY_ENABLED, true);
        }

        public static bool IsSensorSimulationEnabled(Dictionary<string, object> state)
        {
            if (state.ContainsKey(SENSOR_SIMULATION_ENABLED))
            {
                return (bool) state[SENSOR_SIMULATION_ENABLED];
            }

            return true;
        }

        public static bool IsTelemetryEnabled(Dictionary<string, object> state)
        {
            if (state.ContainsKey(TELEMETRY_ENABLED))
            {
                return (bool) state[TELEMETRY_ENABLED];
            }

            return true;
        }
    }
}
