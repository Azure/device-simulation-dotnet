// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub
{
    // retrieves Iot Hub connection secret from storage
    public interface IIotHubConnectionStringManager
    {
        string GetIotHubConnectionString();
        string StoreAndRedact(string connectionString);
        void UseLocalIotHub();
    }

    public class IotHubConnectionStringManager : IIotHubConnectionStringManager
    {
        private const string USE_LOCAL_IOTHUB = "default";
        private const string CONNSTRING_REGEX = @"^HostName=(?<hostName>.*);SharedAccessKeyName=(?<keyName>.*);SharedAccessKey=(?<key>.*)$";
        private const string CONNSTRING_REGEX_HOSTNAME = "hostName";
        private const string CONNSTRING_REGEX_KEYNAME = "keyName";
        private const string CONNSTRING_REGEX_KEY = "key";

        private readonly string connStringFilePath;
            
        private readonly IServicesConfig config;
        private readonly ILogger log;

        public IotHubConnectionStringManager(
            IServicesConfig config,
            ILogger logger)
        {
            // Store file for connection string under the solution directory
            // so all projects have access
            var projectPath = AppContext.BaseDirectory
                .Substring(0, AppContext.BaseDirectory
                .IndexOf("bin", StringComparison.Ordinal));
            var solutionPath = Directory.GetParent(projectPath).Parent.FullName;
            this.connStringFilePath = solutionPath + Path.DirectorySeparatorChar + "custom_iothub_key.txt";

            this.config = config;
            this.log = logger;
        }

        /// <summary>
        /// Checks storage for which connection string to use.
        /// If value is null or file doesn't exist, return the
        /// value stored in environment variable. Otherwise
        /// returns value in local storage.
        /// </summary>
        /// <returns></returns>
        public string GetIotHubConnectionString()
        {
            // read connection string file from webservice
            string customIotHub = this.ReadFromFile(this.connStringFilePath);

            // if no user provided hub is stored, use the pre-provisioned hub 
            if (customIotHub.IsNullOrWhiteSpace())
            {
                this.log.Info("Using IotHub stored in PCS_IOTHUB_CONNSTRING.", () => new { });
                return this.config.IoTHubConnString;
            }

            this.log.Info("Using IoTHub provided by the client.", () => new { });
            return customIotHub;
        }

        /// <summary>
        /// Validates that the IoTHub Connection String is valid, stores the full
        /// string with key in a local file, then removes the sensitive key data and
        /// returns the IoTHub Connection String with and empty string for the SharedAccessKey
        /// 
        /// TODO Encryption for key & storage in documentDb instead of file
        ///      and investigate conversion to SecureString
        ///      https://github.com/Azure/device-simulation-dotnet/issues/129
        /// </summary>
        /// <param name="connectionString"></param>
        public string StoreAndRedact(string connectionString)
        {
            // check if environment variable should be used
            if (connectionString.IsNullOrWhiteSpace() ||
                connectionString == USE_LOCAL_IOTHUB)
            {
                this.UseLocalIotHub();
                return USE_LOCAL_IOTHUB;
            }

            // check that connection strng is valid and the IotHub exists
            this.IsValidConnectionString(connectionString);

            // find key
            var key = GetKeyFromConnString(connectionString);

            // if key is null, the string has been redacted,
            // check if hub is in storage
            if (key.IsNullOrWhiteSpace())
            {
                if (this.ConnectionStringIsStored(connectionString))
                {
                    return connectionString;
                }
                else
                {
                    string message = "Could not connect to IotHub with the connection" +
                                     "string provided. Check that the key is valid and " +
                                     "that the hub exists.";
                    throw new IotHubConnectionException(message);
                }
            }

            // store full connection string with key in local file
            WriteToFile(connectionString, this.connStringFilePath);

            // redact key from connection string and return
            return connectionString.Replace(key, "");
        }

        /// <summary>
        /// If simulation uses the pre-provisioned IoT Hub for the service,
        /// remove sensitive hub information that is no longer needed
        /// </summary>
        public void UseLocalIotHub()
        {
            // delete custom IoT Hub string if local hub is being used
            File.Delete(this.connStringFilePath);
        }

        /// <summary>
        /// Checks if connection string provided has a valid format.
        /// If format is valid, and the connection string has a non-null
        /// value for the key, also checks if a connection to the IotHub
        /// can be made.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public bool IsValidConnectionString(string connectionString)
        {
            var match = Regex.Match(connectionString, CONNSTRING_REGEX);

            if (!match.Success)
            {
                var message = "Invalid connection string format for IoTHub. " +
                    "The correct format is: HostName=[hubname];SharedAccessKeyName=" +
                    "[iothubowner or service];SharedAccessKey=[null or valid key]";

                throw new InvalidIotHubConnectionStringFormatException(message);
            }

            // if a key is provided, check if IoTHub is valid
            if (!match.Groups[CONNSTRING_REGEX_KEY].Value.IsNullOrWhiteSpace())
            {
                try
                {
                    RegistryManager.CreateFromConnectionString(connectionString);
                }
                catch (IOException)
                {
                    string message = "Could not connect to IotHub with the connection" +
                                     "string provided. Check that the key is valid and " +
                                     "that the hub exists.";
                    throw new IotHubConnectionException(message);
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private static string GetKeyFromConnString(string connectionString)
        {
            var match = Regex.Match(connectionString, CONNSTRING_REGEX);

            return match.Groups[CONNSTRING_REGEX_KEY].Value;
        }

        /// <summary>
        /// Takes in a connection string with empty key information.
        /// Returns true if the key for the redacted string is in storage.
        /// Returns false if the key for the redacted string is not in storage.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private bool ConnectionStringIsStored(string connectionString)
        {
            // parse user provided hub info
            var userHubMatch = Regex.Match(connectionString, CONNSTRING_REGEX);
            var userHubHostName = userHubMatch.Groups[CONNSTRING_REGEX_HOSTNAME].Value;
            var userHubKeyName = userHubMatch.Groups[CONNSTRING_REGEX_KEYNAME].Value;

            // parse stored hub info
            var storedHubString = this.ReadFromFile(this.connStringFilePath);
            var storedHubMatch = Regex.Match(storedHubString, CONNSTRING_REGEX);
            var storedHubHostName = storedHubMatch.Groups[CONNSTRING_REGEX_HOSTNAME].Value;
            var storedHubKeyName = storedHubMatch.Groups[CONNSTRING_REGEX_KEYNAME].Value;

            return userHubHostName == storedHubHostName &&
                   userHubKeyName == storedHubKeyName;
        }

        private static void WriteToFile(string connectionString, string path)
        {
            if (!File.Exists(path))
            {
                // Create a file to write to. 
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(connectionString);
                }
            }
            else
            {
                File.WriteAllText(path, connectionString);
            }
        }

        /// <summary>
        /// Retrieves connection string from local storage.
        /// Returns null if file doesn't exist.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string ReadFromFile(string path)
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
