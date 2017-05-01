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
        /// <summary>Web service listening port</summary>
        int Port { get; }

        /// <summary>Service layer configuration</summary>
        Services.Runtime.IConfig ServicesConfig { get; }
    }

    /// <summary>Web service configuration</summary>
    public class Config : IConfig
    {
        private const string Namespace = "com.microsoft.azure.iotsolutions.";
        private const string Application = "device-simulation.";

        public Config()
        {
            var config = ConfigurationFactory.ParseString(GetHoconConfiguration());

            this.Port = config.GetInt(Namespace + Application + "webservice-port");

            this.ServicesConfig = new Services.Runtime.Config
            {
                HubConnString = config.GetString(Namespace + Application + "iothub.connstring")
            };
        }

        /// <summary>Web service listening port</summary>
        public int Port { get; }

        /// <summary>Service layer configuration</summary>
        public Services.Runtime.IConfig ServicesConfig { get; }

        /// <summary>
        /// Read the `application.conf` HOCON file, enabling substitutions of
        /// ${NAME} placeholders with environment variables values.
        /// </summary>
        /// <returns>Configuration text content</returns>
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
