// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Extensions.Configuration;

// TODO: tests
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Runtime
{
    public interface IConfigData
    {
        string GetString(string key);
        int GetInt(string key);
    }

    public class ConfigData : IConfigData
    {
        private readonly IConfigurationRoot configuration;

        public ConfigData()
        {
            // More info about configuration at
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true);

            this.configuration = configurationBuilder.Build();
        }

        public string GetString(string key)
        {
            var value = this.configuration.GetValue<string>(key);
            return ReplaceEnvironmentVariables(value);
        }

        public int GetInt(string key)
        {
            try
            {
                return Convert.ToInt32(this.GetString(key));
            }
            catch (Exception e)
            {
                throw new InvalidConfigurationException($"Unable to load configuration value for '{key}'", e);
            }
        }

        private static string ReplaceEnvironmentVariables(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return ProcessMandatoryPlaceholders(value);
        }

        private static string ProcessMandatoryPlaceholders(string value)
        {
            // Pattern for mandatory replacements: ${VAR_NAME}
            const string pattern = @"\${([a-zA-Z_][a-zA-Z0-9_]*)}";

            // Search
            var keys = (from Match m in Regex.Matches(value, pattern)
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
            keys = (from Match m in Regex.Matches(value, pattern)
                    select m.Groups[1].Value).ToArray();
            if (keys.Length > 0)
            {
                var varsNotFound = keys.Aggregate(", ", (current, k) => current + k);
                throw new InvalidConfigurationException("Environment variables not found: " + varsNotFound);
            }

            return value;
        }
    }
}
