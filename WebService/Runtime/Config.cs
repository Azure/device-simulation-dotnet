// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using Akka.Configuration;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime
{
    public interface IConfig
    {
        int Port { get; }
    }

    public class Config : IConfig
    {
        private const string Namespace = "com.microsoft.azure.iotsolutions.";
        private const string Application = "device-simulation.";

        public Config()
        {
            // Load HOCON and apply env vars substitutions
            var config = ConfigurationFactory.ParseString(GetHoconConfiguration());

            this.Port = config.GetInt(Namespace + Application + "webservice-port");
        }

        public int Port { get; }

        private static string GetHoconConfiguration()
        {
            var hocon = File.ReadAllText("application.conf");

            // Append environment variables to allow Hocon substitutions on them
            var filter = new Regex(@"^[a-zA-Z0-9_/.,:;#(){}^=+~| !@$%&*'[\\\]-]*$");
            hocon += "\n";
            foreach (DictionaryEntry x in Environment.GetEnvironmentVariables())
            {
                if (filter.IsMatch(x.Value.ToString())) hocon += x.Key + " : \"" + x.Value + "\"\n";
            }

            return hocon;
        }
    }
}
