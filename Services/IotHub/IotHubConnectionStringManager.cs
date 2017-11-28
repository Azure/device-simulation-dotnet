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
        string RedactAndStore(string connectionString);
    }

    public class IotHubConnectionStringManager : IIotHubConnectionStringManager
    {
        private const string CONNSTRING_REGEX = @"^HostName=(?<hostName>.*);SharedAccessKeyName=(?<keyName>.*);SharedAccessKey=(?<key>.*)$";
        private const string CONNSTRING_REGEX_HOSTNAME = "hostName";
        private const string CONNSTRING_REGEX_KEYNAME = "keyName";
        private const string CONNSTRING_REGEX_KEY = "key";
        private const string CONNSTRING_FILE_NAME = "custom_iothub_key.txt";

        private readonly string connStringFilePath;
            
        private readonly IServicesConfig config;
        private readonly ILogger log;

        public IotHubConnectionStringManager(
            IServicesConfig config,
            ILogger logger)
        {
            this.config = config;
            this.connStringFilePath = config.IoTHubFolder + CONNSTRING_FILE_NAME;
            this.log = logger;
        }

        /// <summary>
        /// Checks storage for which connection string to use.
        /// If value is null or file doesn't exist, return the
        /// value stored in the configuration file. Otherwise
        /// returns value in local storage.
        /// </summary>
        /// <returns>Full connection string including secret</returns>
        public string GetIotHubConnectionString()
        {
            // read connection string file from webservice
            string customIotHub = this.ReadFromFile();

            // if no user provided hub is stored, use the pre-provisioned hub 
            if (customIotHub.IsNullOrWhiteSpace())
            {
                this.log.Debug("Using IotHub connection string stored in config.", () => { });
                return this.config.IoTHubConnString;
            }

            this.log.Debug("Using IoTHub provided by the client.", () => new { });
            return customIotHub;
        }

        /// <summary>
        /// Validates that the IoTHub Connection String is valid, stores the full
        /// string with key in a local file, then removes the sensitive key data and
        /// returns the IoTHub Connection String with an empty string for the SharedAccessKey
        /// 
        /// TODO Encryption for key & storage in documentDb instead of file
        ///      https://github.com/Azure/device-simulation-dotnet/issues/129
        /// </summary>
        /// <returns>Redacted connection string (i.e. without SharedAccessKey)</returns>
        public string RedactAndStore(string connectionString)
        {
            // check if environment variable should be used
            if (connectionString.IsNullOrWhiteSpace() ||
                connectionString == ServicesConfig.USE_DEFAULT_IOTHUB)
            {
                this.UseDefaultIotHub();
                return ServicesConfig.USE_DEFAULT_IOTHUB;
            }

            // check that connection strng is valid and the IotHub exists
            this.ValidateConnectionString(connectionString);

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
            this.WriteToFile(connectionString);

            // redact key from connection string and return
            return connectionString.Replace(key, "");
        }

        /// <summary>
        /// Checks if connection string provided has a valid format.
        /// If format is valid, and the connection string has a non-null
        /// value for the key, also checks if a connection to the IotHub
        /// can be made.
        /// </summary>
        public void ValidateConnectionString(string connectionString)
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
        }

        /// <summary>
        /// If simulation uses the pre-provisioned IoT Hub for the service,
        /// then remove sensitive hub information that is no longer needed
        /// </summary>
        private void UseDefaultIotHub()
        {
            try
            {
                // delete custom IoT Hub string if default hub is being used
                File.Delete(this.connStringFilePath);
            }
            catch (Exception e)
            {
                this.log.Error("Unable to delete connection string file.", () => new { this.connStringFilePath, e });
                throw;
            }
        }

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
        private bool ConnectionStringIsStored(string connectionString)
        {
            // parse user provided hub info
            var userHubMatch = Regex.Match(connectionString, CONNSTRING_REGEX);
            var userHubHostName = userHubMatch.Groups[CONNSTRING_REGEX_HOSTNAME].Value;
            var userHubKeyName = userHubMatch.Groups[CONNSTRING_REGEX_KEYNAME].Value;

            // parse stored hub info
            var storedHubString = this.ReadFromFile();
            var storedHubMatch = Regex.Match(storedHubString, CONNSTRING_REGEX);
            var storedHubHostName = storedHubMatch.Groups[CONNSTRING_REGEX_HOSTNAME].Value;
            var storedHubKeyName = storedHubMatch.Groups[CONNSTRING_REGEX_KEYNAME].Value;

            return userHubHostName == storedHubHostName &&
                   userHubKeyName == storedHubKeyName;
        }

        private void WriteToFile(string connectionString)
        {
            this.log.Debug("Write IotHub connection string to file.", () => new { this.connStringFilePath });

            try
            {
                File.WriteAllText(this.connStringFilePath, connectionString);
            }
            catch (Exception e)
            {
                this.log.Error("Unable to write connection string to file.", () => new { this.connStringFilePath, e });
                throw;
            }
        }

        /// <summary>
        /// Retrieves connection string from local storage.
        /// Returns null if file doesn't exist.
        /// </summary>
        private string ReadFromFile()
        {
            this.log.Debug("Check for IotHub connection string from file.", () => new { this.connStringFilePath });
            if (File.Exists(this.connStringFilePath))
            {
                try
                {
                    // remove special characters and return string
                    return Regex.Replace(File.ReadAllText(this.connStringFilePath), @"[\r\n\t ]+", "");
                }
                catch (Exception e)
                {
                    this.log.Error("Unable to read connection string from file.", () => new { this.connStringFilePath, e });
                    return null;
                }
            }

            this.log.Debug("IotHub connection string file not present.", () => new { this.connStringFilePath });
            return null;
        }
    }
}
