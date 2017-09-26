// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    public interface IConfigData
    {
        string GetString(string key, string defaultValue = "");
        bool GetBool(string key, bool defaultValue = false);
        int GetInt(string key, int defaultValue = 0);
    }

    public class ConfigData : IConfigData
    {
        private readonly IConfigurationRoot configuration;
        private readonly ILogger log;

        public ConfigData(ILogger logger)
        {
            this.log = logger;

            // More info about configuration at
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true);
            this.configuration = configurationBuilder.Build();
        }

        public string GetString(string key, string defaultValue = "")
        {
            var value = this.configuration.GetValue(key, defaultValue);
            this.ReplaceEnvironmentVariables(ref value, defaultValue);
            return value;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            var value = this.GetString(key, defaultValue.ToString()).ToLowerInvariant();

            var knownTrue = new HashSet<string> { "true", "t", "yes", "y", "1", "-1" };
            var knownFalse = new HashSet<string> { "false", "f", "no", "n", "0" };

            if (knownTrue.Contains(value)) return true;
            if (knownFalse.Contains(value)) return false;

            return defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            try
            {
                return Convert.ToInt32(this.GetString(key, defaultValue.ToString()));
            }
            catch (Exception e)
            {
                throw new InvalidConfigurationException($"Unable to load configuration value for '{key}'", e);
            }
        }

        private void ReplaceEnvironmentVariables(ref string value, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(value)) return;

            this.ProcessMandatoryPlaceholders(ref value);

            this.ProcessOptionalPlaceholders(ref value, out bool notFound);

            if (notFound && string.IsNullOrEmpty(value))
            {
                value = defaultValue;
            }
        }

        private void ProcessMandatoryPlaceholders(ref string value)
        {
            // Pattern for mandatory replacements: ${VAR_NAME}
            const string PATTERN = @"\${([a-zA-Z_][a-zA-Z0-9_]*)}";

            // Search
            var keys = (from Match m in Regex.Matches(value, PATTERN)
                        select m.Groups[1].Value).Distinct().ToArray();

            // Replace
            foreach (DictionaryEntry x in Environment.GetEnvironmentVariables())
            {
                if (keys.Contains(x.Key))
                {
                    value = value.Replace("${" + x.Key + "}", x.Value.ToString());
                }
            }

            // Non replaced placeholders cause an exception
            keys = (from Match m in Regex.Matches(value, PATTERN)
                    select m.Groups[1].Value).ToArray();
            if (keys.Length > 0)
            {
                var varsNotFound = keys.Aggregate(", ", (current, k) => current + k);
                this.log.Error("Environment variables not found", () => new { varsNotFound });
                throw new InvalidConfigurationException("Environment variables not found: " + varsNotFound);
            }
        }

        private void ProcessOptionalPlaceholders(ref string value, out bool notFound)
        {
            notFound = false;

            // Pattern for optional replacements: ${?VAR_NAME}
            const string PATTERN = @"\${\?([a-zA-Z_][a-zA-Z0-9_]*)}";

            // Search
            var keys = (from Match m in Regex.Matches(value, PATTERN)
                        select m.Groups[1].Value).Distinct().ToArray();

            // Replace
            foreach (DictionaryEntry x in Environment.GetEnvironmentVariables())
            {
                if (keys.Contains(x.Key))
                {
                    value = value.Replace("${?" + x.Key + "}", x.Value.ToString());
                }
            }

            // Non replaced placeholders cause an exception
            keys = (from Match m in Regex.Matches(value, PATTERN)
                    select m.Groups[1].Value).ToArray();
            if (keys.Length > 0)
            {
                // Remove placeholders
                value = keys.Aggregate(value, (current, k) => current.Replace("${?" + k + "}", string.Empty));

                var varsNotFound = keys.Aggregate(", ", (current, k) => current + k);
                this.log.Warn("Environment variables not found", () => new { varsNotFound });

                notFound = true;
            }
        }
    }
}
