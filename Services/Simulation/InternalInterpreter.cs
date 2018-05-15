// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
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
        /// <param name="properties">Current device properties state</param>
        /// <remarks>Updates the internal device sensors state</remarks>
        void Invoke(
            string scriptPath, object scriptParams,
            Dictionary<string, object> context,
            ISmartDictionary state,
            ISmartDictionary properties);
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

        public void Invoke(
            string scriptPath,
            object scriptParams,
            Dictionary<string, object> context,
            ISmartDictionary state,
            ISmartDictionary properties)
        {
            switch (scriptPath.ToLowerInvariant())
            {
                case SCRIPT_RANDOM:
                    this.RunRandomNumberScript(scriptParams, state);
                    break;
                case SCRIPT_INCREASING:
                    this.RunIncreasingScript(scriptParams, state);
                    break;
                case SCRIPT_DECREASING:
                    this.RunDecreasingScript(scriptParams, state);
                    break;
                default:
                    throw new NotSupportedException($"Unknown script `{scriptPath}`.");
            }
        }

        // For each sensors specified, generate a random number in the range requested
        private void RunRandomNumberScript(object scriptParams, ISmartDictionary state)
        {
            var sensors = this.JsonParamAsDictionary(scriptParams);
            foreach (var sensor in sensors)
            {
                (double min, double max) = this.GetMinMaxParameters(sensor.Value);
                var value = this.random.NextDouble() * (max - min) + min;
                state.Set(sensor.Key, value);
            }
        }

        // For each sensors specified, increase the current state, up to a maximum, then restart from a minimum
        private void RunIncreasingScript(object scriptParams, ISmartDictionary state)
        {
            var sensors = this.JsonParamAsDictionary(scriptParams);
            foreach (var sensor in sensors)
            {
                // Extract scripts parameters from the device model script configuration
                (double min, double max, double step) = this.GetMinMaxStepParameters(sensor.Value);

                // Add the sensor to the state if missing
                if (!state.Has(sensor.Key))
                {
                    state.Set(sensor.Key, min);
                }

                double current = Convert.ToDouble(state.Get(sensor.Key));
                double next = AreEqual(current, max) ? min : Math.Min(current + step, max);

                state.Set(sensor.Key, next);
            }
        }

        // For each sensors specified, decrease the current state, down to a minimum, then restart from a maximum
        private void RunDecreasingScript(object scriptParams, ISmartDictionary state)
        {
            var sensors = this.JsonParamAsDictionary(scriptParams);
            foreach (var sensor in sensors)
            {
                // Extract scripts parameters from the device model script configuration
                (double min, double max, double step) = this.GetMinMaxStepParameters(sensor.Value);

                // Add the sensor to the state if missing
                if (!state.Has(sensor.Key))
                {
                    state.Set(sensor.Key, max);
                }

                double current = Convert.ToDouble(state.Get(sensor.Key));
                double next = AreEqual(current, min) ? max : Math.Max(current - step, min);

                state.Set(sensor.Key, next);
            }
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

        private static bool AreEqual(double a, double b)
        {
            return Math.Abs(a - b) < EQUALITY_PRECISION;
        }
    }
}
