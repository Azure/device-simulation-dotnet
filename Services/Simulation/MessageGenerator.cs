// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation
{
    public interface IMessageGenerator
    {
        string Generate(
            DeviceType deviceType,
            string template,
            string previousMessage,
            string deviceId);
    }

    public class MessageGenerator : IMessageGenerator
    {
        /// <summary>
        /// Regex used to extract placeholders from the message templates
        /// Example:
        /// "{\"current_floor\": ${get_current_floor.value}}"
        /// => get_current_floor.value
        /// </summary>
        private const string PlaceholderPattern =
            @"\${([a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z_][a-zA-Z0-9_]*)}";

        private readonly IJavascriptInterpreter jsInterpreter;
        private readonly ILogger log;

        public MessageGenerator(
            IJavascriptInterpreter jsInterpreter,
            ILogger logger)
        {
            this.jsInterpreter = jsInterpreter;
            this.log = logger;
        }

        /// <summary>
        /// Generate messages from a template.
        /// Template examples::
        /// "{\"speed\":${get_truck_speed.value},\"geolocation\":\"${get_truck_location.value}\",\"speed_unit\":\"mph\"}"
        /// "{\"current_floor\": ${get_current_floor.value}}"
        /// </summary>
        public string Generate(
            DeviceType deviceType,
            string template,
            string previousMessage,
            string deviceId)
        {
            var result = template;

            var placeholders = this.ExtractPlaceholders(template);
            var functions = this.ExtractFunctions(placeholders);
            var data = this.InvokeFunctions(functions, deviceType.DeviceBehavior, deviceId);

            // TODO

            return result;
        }

        /// <summary>
        /// Extract all the placeholders from the message template
        /// </summary>
        private string[] ExtractPlaceholders(string template)
        {
            return (from Match m
                        in Regex.Matches(template, PlaceholderPattern)
                    select m.Groups[1].Value).Distinct().ToArray();
        }

        /// <summary>
        /// Build a map to know which functions need to be called.
        /// The key is the function name.
        /// The value is a list of placeholders that will use the function output.
        /// Note: functions are called only once, and only if needed.
        /// </summary>
        private IDictionary<string, string[]> ExtractFunctions(string[] placeholders)
        {
            var result = new Dictionary<string, string[]>();

            foreach (var p in placeholders)
            {
                var pos = p.IndexOf('.');
                if (pos < 0) continue;

                var functionName = p.Substring(0, pos);
                if (result.ContainsKey(functionName))
                {
                    result[functionName].Append(p);
                }
                else
                {
                    result[functionName] = new[] { p };
                }
            }

            return result;
        }

        /// <summary>
        /// Invoke all the functions required and returns a table mapping
        /// a placeholder to a value.
        /// </summary>
        private IDictionary<string, string> InvokeFunctions(
            IDictionary<string, string[]> functions,
            IDictionary<string, DeviceType.DeviceTypeFunction> behavior,
            string deviceId)
        {
            var result = new Dictionary<string, string>();

            foreach (var f in functions)
            {
                if (!behavior.ContainsKey(f.Key))
                {
                    this.log.Error("The message template references an unknown function",
                        () => new { Function = f.Key, deviceId });
                    throw new NotSupportedException(
                        $"The message template references an unknown function `{f.Key}`.");
                }

                var function = behavior[f.Key];
                if (function.Type.ToLowerInvariant() != "javascript")
                {
                    this.log.Error("The device type behavior is of an unknown type",
                        () => new { function.Type, deviceId });
                    throw new NotSupportedException(
                        $"The device type behavior is of an unknown type `${function.Type}`.");
                }

                var data = this.jsInterpreter.Invoke(
                    function.Path, deviceId, DateTimeOffset.UtcNow);
            }

            return result;
        }
    }
}
