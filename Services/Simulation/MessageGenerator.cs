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
        /// <summary>
        /// Each simulated device has a dedicated message generator,
        /// that needs to be prepared once.
        /// </summary>
        void Setup(
            DeviceType deviceType,
            string template,
            string deviceId);

        string GetNext();
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

        private DeviceType deviceType;
        private string template;
        private string deviceId;
        private string[] placeholders;
        private IDictionary<string, string[]> functions;
        private Dictionary<string, Dictionary<string, string>> functionsResult;

        public MessageGenerator(
            IJavascriptInterpreter jsInterpreter,
            ILogger logger)
        {
            this.jsInterpreter = jsInterpreter;
            this.log = logger;
        }

        /// <summary>
        /// Each simulated device has a dedicated message generator,
        /// that needs to be prepared once.
        /// </summary>
        public void Setup(
            DeviceType deviceType,
            string template,
            string deviceId)
        {
            this.deviceType = deviceType;
            this.template = template;
            this.deviceId = deviceId;

            this.placeholders = ExtractPlaceholders(template);
            this.functions = ExtractFunctions(this.placeholders);

            // Initialize the functions results table with empty data
            this.functionsResult = new Dictionary<string, Dictionary<string, string>>();
            foreach (var f in this.functions)
            {
                if (!this.deviceType.DeviceBehavior.ContainsKey(f.Key))
                {
                    this.log.Error("The message template references an unknown function",
                        () => new { Function = f.Key, this.deviceId });
                    throw new NotSupportedException(
                        $"The message template references an unknown function `{f.Key}`.");
                }

                this.functionsResult.Add(f.Key, null);
            }
        }

        /// <summary>
        /// Generate messages from a template, replacing placeholders with
        /// values provided by external functions.
        ///
        /// Template examples::
        /// "{\"speed\":${get_truck_speed.value},\"geolocation\":\"${get_truck_location.value}\",\"speed_unit\":\"mph\"}"
        /// "{\"current_floor\": ${get_current_floor.value}}"
        /// </summary>
        public string GetNext()
        {
            this.InvokeFunctions();

            var result = this.template;
            foreach (var function in this.functionsResult)
            {
                var functionName = function.Key;
                foreach (var resultField in function.Value)
                {
                    result = result.Replace("${" + functionName + "." + resultField.Key + "}", resultField.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// Invoke all the functions required to render the template.
        /// For each function call, the function receives in input some
        /// context details, like the current time and the Device Id, plus
        /// the output from the previous call. The previous result can be
        /// useful for the function to maintain some internal state.
        /// </summary>
        private void InvokeFunctions()
        {
            foreach (var f in this.functions)
            {
                var function = this.deviceType.DeviceBehavior[f.Key];

                switch (function.Type.ToLowerInvariant())
                {
                    case "javascript":
                        this.functionsResult[f.Key] = this.jsInterpreter.Invoke(
                            function.Path,
                            this.deviceId,
                            DateTimeOffset.UtcNow,
                            this.functionsResult[f.Key]);
                        break;

                    default:
                        this.log.Error("The device type behavior is of an unknown type",
                            () => new { function.Type, this.deviceId });
                        throw new NotSupportedException(
                            $"The device type behavior is of an unknown type `${function.Type}`.");
                }
            }
        }

        /// <summary>
        /// Extract all the placeholders from the message template
        /// </summary>
        private static string[] ExtractPlaceholders(string template)
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
        private static IDictionary<string, string[]> ExtractFunctions(string[] placeholders)
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
    }
}
