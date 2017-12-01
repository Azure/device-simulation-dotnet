// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation
{
    public interface IInternalInterpreter
    {
        // List of supported scripts, used for input validation
        HashSet<string> SupportedScripts { get; }

        /// <summary>Invoke one of the internal scripts</summary>
        /// <param name="scriptPath">Name of the script</param>
        /// <param name="scriptParams">Script parameters, e.g. min, max, step</param>
        /// <param name="context">Context, e.g. current time, device Id, device Model</param>
        /// <param name="state">Current device sensors state</param>
        /// <returns>New device sensors state</returns>
        Dictionary<string, object> Invoke(
            string scriptPath, object scriptParams,
            Dictionary<string, object> context,
            Dictionary<string, object> state);
    }

    public class InternalInterpreter : IInternalInterpreter
    {
        private const string SCRIPT_RANDOM = "math.random.withinrange";
        private const string SCRIPT_INCREASING = "math.increasing";
        private const string SCRIPT_DECREASING = "math.decreasing";
        private const double EQUALITY_PRECISION = .001;

        public HashSet<string> SupportedScripts => new HashSet<string>
        {
            SCRIPT_RANDOM,
            SCRIPT_INCREASING,
            SCRIPT_DECREASING
        };

        private readonly Random random;
        private readonly ILogger log;

        public InternalInterpreter(ILogger logger)
        {
            this.log = logger;
            this.random = new Random();
        }

        public Dictionary<string, object> Invoke(
            string scriptPath,
            object scriptParams,
            Dictionary<string, object> context,
            Dictionary<string, object> state)
        {
            switch (scriptPath.ToLowerInvariant())
            {
                case SCRIPT_RANDOM:
                    return this.RunRandomNumberScript(scriptParams, state);

                case SCRIPT_INCREASING:
                    return this.RunIncreasingScript(scriptParams, state);

                case SCRIPT_DECREASING:
                    return this.RunDecreasingScript(scriptParams, state);

                default:
                    throw new NotSupportedException($"Unknown script `{scriptPath}`.");
            }
        }

        // For each sensors specified, generate a random number in the range requested
        private Dictionary<string, object> RunRandomNumberScript(object scriptParams, Dictionary<string, object> state)
        {
            var sensors = this.JsonParamAsDictionary(scriptParams);
            foreach (var sensor in sensors)
            {
                (double min, double max) = this.GetMinMaxParameters(sensor.Value);
                state[sensor.Key] = this.random.NextDouble() * (max - min) + min;
            }

            return state;
        }

        // For each sensors specified, increase the current state, up to a maximum, then restart from a minimum
        private Dictionary<string, object> RunIncreasingScript(object scriptParams, Dictionary<string, object> state)
        {
            var sensors = this.JsonParamAsDictionary(scriptParams);
            foreach (var sensor in sensors)
            {
                // Extract scripts parameters from the device model script configuration
                (double min, double max, double step) = this.GetMinMaxStepParameters(sensor.Value);

                // Add the sensor to the state if missing
                if (!state.ContainsKey(sensor.Key))
                {
                    state[sensor.Key] = min;
                }

                double current = Convert.ToDouble(state[sensor.Key]);
                double next = Math.Abs(current - max) < EQUALITY_PRECISION ? min : Math.Min(current + step, max);

                state[sensor.Key] = next;
            }

            return state;
        }

        // For each sensors specified, increase the current state, up to a maximum, then restart from a minimum
        private Dictionary<string, object> RunDecreasingScript(object scriptParams, Dictionary<string, object> state)
        {
            var sensors = this.JsonParamAsDictionary(scriptParams);
            foreach (var sensor in sensors)
            {
                // Extract scripts parameters from the device model script configuration
                (double min, double max, double step) = this.GetMinMaxStepParameters(sensor.Value);

                // Add the sensor to the state if missing
                if (!state.ContainsKey(sensor.Key))
                {
                    state[sensor.Key] = max;
                }

                double current = Convert.ToDouble(state[sensor.Key]);
                double next = Math.Abs(current - max) < EQUALITY_PRECISION ? max : Math.Max(current - step, min);

                state[sensor.Key] = next;
            }

            return state;
        }

        private (double, double) GetMinMaxParameters(object parameters)
        {
            var dict = this.JsonParamAsDictionary(parameters);

            if (!dict.ContainsKey("Min"))
            {
                this.log.Error("Missing 'Min' parameter", () => new { dict });
                throw new FormatException("Missing 'Min' parameter");
            }

            if (!dict.ContainsKey("Max"))
            {
                this.log.Error("Missing 'Max' parameter", () => new { dict });
                throw new FormatException("Missing 'Max' parameter");
            }

            return (Convert.ToDouble(dict["Min"]), Convert.ToDouble(dict["Max"]));
        }

        private (double, double, double) GetMinMaxStepParameters(object parameters)
        {
            var dict = this.JsonParamAsDictionary(parameters);

            if (!dict.ContainsKey("Min"))
            {
                this.log.Error("Missing 'Min' parameter", () => new { dict });
                throw new FormatException("Missing 'Min' parameter");
            }

            if (!dict.ContainsKey("Max"))
            {
                this.log.Error("Missing 'Max' parameter", () => new { dict });
                throw new FormatException("Missing 'Max' parameter");
            }

            if (!dict.ContainsKey("Step"))
            {
                this.log.Error("Missing 'Step' parameter", () => new { dict });
                throw new FormatException("Missing 'Step' parameter");
            }

            return (Convert.ToDouble(dict["Min"]),
                Convert.ToDouble(dict["Max"]),
                Convert.ToDouble(dict["Step"]));
        }

        private Dictionary<string, object> JsonParamAsDictionary(object parameters)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(parameters));
            }
            catch (Exception e)
            {
                this.log.Error("Unknown script parameters format. The parameters should be passed key-value dictionary.", () => new { parameters, e });
                throw new NotSupportedException("Unknown script parameters format. The parameters should be passed key-value dictionary.");
            }
        }
    }
}
