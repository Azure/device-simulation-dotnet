// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;

// @TODO - Support multiple connection string, one or more per simulation
//         Currently there's only one global connection string
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub
{
    public interface IConnectionStrings
    {
        Task<string> GetAsync();
        Task<string> SaveAsync(string connectionString, bool validateHubCredentials);
    }

    public class ConnectionStrings : IConnectionStrings
    {
        private const string RECORD_ID = "custom_iothub_key";

        private readonly IServicesConfig config;
        private readonly IConnectionStringValidation connectionStringValidation;
        private readonly ILogger log;
        private readonly IDiagnosticsLogger diagnosticsLogger;
        private readonly IEngine mainStorage;

        public ConnectionStrings(
            IServicesConfig config,
            IConnectionStringValidation connectionStringValidation,
            IEngines engines,
            IDiagnosticsLogger diagnosticsLogger,
            ILogger logger)
        {
            this.config = config;
            this.connectionStringValidation = connectionStringValidation;
            this.mainStorage = engines.Build(config.MainStorage);
            this.log = logger;
            this.diagnosticsLogger = diagnosticsLogger;
        }

        /// <summary>
        /// Checks storage for which connection string to use.
        /// If record exists and is not empty return it, otherwise
        /// return the value stored in service configuration file.
        /// </summary>
        /// <returns>Full connection string (i.e. with SharedAccessKey)</returns>
        public async Task<string> GetAsync()
        {
            string customIotHub = await this.ReadConnectionStringFromStorageAsync();

            // Check if the pre-provisioned IoT Hub should be used
            if (this.connectionStringValidation.IsEmptyOrDefault(customIotHub))
            {
                this.log.Debug("Using the Iot Hub connection string specified in the config file.");

                // TODO: there's an edge case where the user is asking to use the secondary key
                // because the primary has been changed, and this logic will cause an error
                return this.config.IoTHubConnString;
            }

            this.log.Debug("Using IoT Hub connection string provided by the user.");
            return customIotHub;
        }

        /// <summary>
        /// Validates that the IoTHub Connection String is valid, stores the full
        /// string with key in storage, then removes the sensitive key data and
        /// returns the IoTHub Connection String with an empty string for the SharedAccessKey
        /// 
        /// TODO: use KeyVault https://github.com/Azure/device-simulation-dotnet/issues/129
        /// </summary>
        /// <returns>Redacted connection string (i.e. without SharedAccessKey)</returns>
        public async Task<string> SaveAsync(string connectionString, bool validateHubCredentials)
        {
            // Check if configuration setting should be used
            if (this.connectionStringValidation.IsEmptyOrDefault(connectionString))
            {
                if (validateHubCredentials)
                {
                    await this.connectionStringValidation.TestAsync(this.config.IoTHubConnString, false);
                }

                await this.RemoveCustomConnStringFromStorageAsync();
                return ServicesConfig.USE_DEFAULT_IOTHUB;
            }
            else
            {
                if (validateHubCredentials)
                {
                    // Check that connection string is valid and the IotHub exists
                    await this.connectionStringValidation.TestAsync(connectionString, true);
                }
            }

            // If the secret key is missing, the string has been redacted,
            // so check if the full connection string is in storage.
            // This happens when the UI sends the default connection string (from config) without
            // secret, to avoid leaking that secret to unauthorized users.
            var secret = this.GetSecretKeyFromConnString(connectionString);
            if (string.IsNullOrWhiteSpace(secret))
            {
                if (!await this.ConnectionStringIsStoredAsync(connectionString))
                {
                    const string MSG = "Could not connect to IotHub with the connection " +
                                       "string provided. Check that the key is provided or in storage.";
                    this.log.Error(MSG);
                    throw new IotHubConnectionException(MSG);
                }

                return connectionString;
            }
            else
            {
                await this.WriteConnectionStringToStorageAsync(connectionString);

                // Redact secret key from connection string and return
                return connectionString.Replace(secret, "");
            }
        }

        // If the simulation uses the pre-provisioned IoT Hub,
        // then remove stored sensitive hub information that is no longer needed
        private async Task RemoveCustomConnStringFromStorageAsync()
        {
            try
            {
                // If the pre-provisioned hub is being used, then delete the custom connection string from storage
                await this.mainStorage.DeleteAsync(RECORD_ID);
            }
            catch (Exception e)
            {
                var msg = "Unable to delete connection string";
                this.log.Error(msg, e);
                this.diagnosticsLogger.LogServiceError(msg, new { e.Message });
                throw;
            }
        }

        private string GetSecretKeyFromConnString(string connectionString)
        {
            var data = this.connectionStringValidation.Parse(connectionString, true);
            return data.keyValue;
        }

        // Takes in a connection string with empty key information.
        // Returns true if the key for the redacted string is in storage.
        // Returns false if the key for the redacted string is not in storage.
        private async Task<bool> ConnectionStringIsStoredAsync(string connectionString)
        {
            // get stored string from storage
            var storedValue = await this.ReadConnectionStringFromStorageAsync();

            var input = this.connectionStringValidation.Parse(connectionString, true);
            var stored = this.connectionStringValidation.Parse(storedValue, true);

            return input.host == stored.host && input.keyName == stored.keyName;
        }

        private async Task WriteConnectionStringToStorageAsync(string connectionString)
        {
            this.log.Debug("Writing Iot Hub connection string to storage");

            try
            {
                var record = this.mainStorage.BuildRecord(RECORD_ID, connectionString);
                await this.mainStorage.UpsertAsync(record);
            }
            catch (Exception e)
            {
                const string MSG = "Unable to write connection string to storage.";
                this.log.Error(MSG, e);
                this.diagnosticsLogger.LogServiceError(MSG, e.Message);
                throw;
            }
        }

        /// <summary>
        /// Retrieves connection string from storage.
        /// Returns null if record doesn't exist.
        /// TODO: store into the simulation record, each simulation has one or more hubs
        /// </summary>
        private async Task<string> ReadConnectionStringFromStorageAsync()
        {
            this.log.Debug("Retrieving Iot Hub connection string from storage.");

            try
            {
                var record = await this.mainStorage.GetAsync(RECORD_ID);
                return record.GetData();
            }
            catch (ResourceNotFoundException)
            {
                this.log.Debug("Iot Hub connection string record not present.");
                return null;
            }
            catch (Exception e)
            {
                const string MSG = "Unable to read connection string from storage";
                this.log.Error(MSG, e);
                this.diagnosticsLogger.LogServiceError(MSG, new { e, e.Message });
                return null;
            }
        }
    }
}
