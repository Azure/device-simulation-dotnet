// Copyright (c) Microsoft. All rights reserved.

using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub
{
    // retrieves Iot Hub connection secret from storage
    public class IotHubConnectionStringManager
    {
        private const string CONNECTION_STRING_FILE_PATH = @"keys\user_iothub_key.txt";

        private readonly IServicesConfig config;
        private readonly ILogger log;

        public IotHubConnectionStringManager(
            IServicesConfig config,
            ILogger logger)
        {
            this.config = config;
            this.log = logger;
        }

        /// <summary>
        /// Checks storage for which connection string to use.
        /// If value is null or file doesn't exist, return the
        /// value stored in PCS_IOTHUB_CONNSTRING. Otherwise
        /// returns value in local storage.
        /// </summary>
        /// <returns></returns>
        public string GetIotHubConnectionString()
        {
            // read connection string file from webservice
            string customIotHub = ReadFromFile(CONNECTION_STRING_FILE_PATH);

            // if no user provided hub is stored, use the pre-provisioned hub 
            if (customIotHub.IsNullOrWhiteSpace())
            {
                this.log.Info("Using IotHub stored in PCS_IOTHUB_CONNSTRING.", () => new { });
                return this.config.IoTHubConnString;
            }

            return customIotHub;
        }

        /// <summary>
        /// Retrieves connection string from local storage.
        /// Returns null if file doesn't exist.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string ReadFromFile(string path)
        {
            if (File.Exists(path))
            {
                // remove special characters and return string
                return Regex.Replace(File.ReadAllText(path), @"[\r\n\t ]+", "");
            }

            return null;
        }
    }
}
