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
        uint GetUInt(string key, uint defaultValue = 0);
        uint? GetOptionalUInt(string key);
    }

    public class ConfigData : IConfigData
    {
        private readonly IConfigurationRoot configuration;
        private readonly ILogger log;

        public ConfigData(IConfigurationRoot configuration, ILogger logger)
        {
            this.log = logger;
            this.configuration = configuration;
        }

        public string GetString(string key, string defaultValue = "")
        {
            return this.GetStringInternal(key, defaultValue);
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            var value = this.GetStringInternal(key, defaultValue.ToString());
            var lcValue = value.ToLowerInvariant();

            var knownTrue = new HashSet<string> { "true", "t", "yes", "y", "1", "-1" };
            var knownFalse = new HashSet<string> { "false", "f", "no", "n", "0", "" };

            if (knownTrue.Contains(lcValue)) return true;
            if (knownFalse.Contains(lcValue)) return false;

            throw new InvalidConfigurationException($"Unable to load configuration value for '{key}' (found: '{value}')");
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            string value = string.Empty;
            try
            {
                value = this.GetStringInternal(key, defaultValue.ToString());
                return Convert.ToInt32(value);
            }
            catch (Exception e)
            {
                throw new InvalidConfigurationException($"Unable to load configuration value for '{key}' (found: '{value}')", e);
            }
        }

        public uint GetUInt(string key, uint defaultValue = 0)
        {
            string value = string.Empty;
            try
            {
                value = this.GetStringInternal(key, defaultValue.ToString());
                return Convert.ToUInt32(value);
            }
            catch (Exception e)
            {
                throw new InvalidConfigurationException($"Unable to load configuration value for '{key}' (found: '{value}')", e);
            }
        }

        public uint? GetOptionalUInt(string key)
        {
            string value = string.Empty;
            try
            {
                var notFound = "NOT.FOUND." + Guid.NewGuid().ToString("N") + ".NOT.FOUND";
                value = this.GetStringInternal(key, notFound);

                if (value == notFound)
                {
                    return null;
                }

                return Convert.ToUInt32(value);
            }
            catch (Exception e)
            {
                throw new InvalidConfigurationException($"Unable to load configuration value for '{key}' (found: '{value}')", e);
            }
        }

        // Try to get a setting, logging when the value is not found
        private string GetStringInternal(string key, string defaultValue)
        {
            var notFound = "NOT.FOUND." + Guid.NewGuid().ToString("N") + ".NOT.FOUND";
            var value = this.configuration.GetValue(key, notFound);
            this.ReplaceEnvironmentVariables(ref value, defaultValue);

            if (value != notFound) return value;
            this.log.Info("Configuration setting not found, using default value", () => new { key, defaultValue });
            return defaultValue;
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

            // Non replaced placeholders are removed
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
