// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub
{
    // retrieves Iot Hub connection secret from storage
    public class IotHubConnectionStringManager
    {
        private const string USE_LOCAL_IOTHUB = "pre-provisioned";
        private const string CONNECTION_STRING_FILE_PATH = @"custom_iothub_key.txt";

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
        /// If value is "pre-provisioned" will return the value
        /// stored in PCS_IOTHUB_CONNSTRING
        /// Otherwise returns value in local storage.
        /// </summary>
        /// <returns></returns>
        public string GetIotHubConnectionString()
        {
            string customIotHub = ReadFromFile(CONNECTION_STRING_FILE_PATH);

            if (customIotHub == USE_LOCAL_IOTHUB)
            {
                return this.config.IoTHubConnString;
            }

            return customIotHub;
        }

        /// <summary>
        /// Retrieves connection string from local storage.
        /// Returns default value if file doesn't exist.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string ReadFromFile(string path)
        {
            string result = USE_LOCAL_IOTHUB;

            if (!File.Exists(path))
            {
                result = File.ReadAllText(path);
            }

            return result;
        }
    }
}
