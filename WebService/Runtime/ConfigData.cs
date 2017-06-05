// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;

// TODO: tests
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime
{
    public interface IConfigData
    {
        string GetString(string key);
        int GetInt(string key);
    }

    public class ConfigData : IConfigData
    {
        public string GetString(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            return ReplaceEnvironmentVariables(value);
        }

        public int GetInt(string key)
        {
            return Convert.ToInt32(this.GetString(key));
        }

        private static string ReplaceEnvironmentVariables(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            // Extract the name of all the substitutions required
            // using the following pattern, e.g. ${VAR_NAME}
            const string pattern = @"\${(?'key'[a-zA-Z_][a-zA-Z0-9_]*)}";
            var keys = (from Match m
                        in Regex.Matches(value, pattern)
                        select m.Groups[1].Value).ToArray();

            foreach (DictionaryEntry x in Environment.GetEnvironmentVariables())
            {
                if (keys.Contains(x.Key))
                {
                    value = value.Replace("${" + x.Key + "}", x.Value.ToString());
                }
            }

            return value;
        }
    }
}
