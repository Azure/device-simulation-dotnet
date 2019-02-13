// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub
{
    public interface IConnectionStringValidation
    {
        // Return true if the string represents the default pre-provisioned IoT Hub
        bool IsEmptyOrDefault(string connectionString);

        // Parse the connection string and validate the format
        (string host, string keyName, string keyValue)
            Parse(string connectionString, bool keyCanBeEmpty);

        // Check if the connection string allows to connect and provides the required access
        Task TestAsync(string connectionString, bool keyCanBeEmpty);
    }

    public class ConnectionStringValidation : IConnectionStringValidation
    {
        private const string REGEX = @"^HostName=(?<hostName>.*);SharedAccessKeyName=(?<keyName>.*);SharedAccessKey=(?<key>.*)$";
        private const string REGEX_HOSTNAME = "hostName";
        private const string REGEX_KEY_NAME = "keyName";
        private const string REGEX_KEY_VALUE = "key";

        private readonly IFactory factory;
        private readonly ILogger log;
        private readonly IDiagnosticsLogger diagnosticsLogger;

        public ConnectionStringValidation(
            IFactory factory,
            IDiagnosticsLogger diagnosticsLogger,
            ILogger logger)
        {
            this.factory = factory;
            this.log = logger;
            this.diagnosticsLogger = diagnosticsLogger;
        }

        // Return true if the string represents the default pre-provisioned IoT Hub
        public bool IsEmptyOrDefault(string connectionString)
        {
            return
                connectionString == null ||
                connectionString.Trim() == string.Empty ||
                string.Equals(connectionString.Trim(), ServicesConfig.USE_DEFAULT_IOTHUB, StringComparison.InvariantCultureIgnoreCase);
        }

        // Parse the connection string and validate the format
        public (string host, string keyName, string keyValue)
            Parse(string connectionString, bool keyCanBeEmpty)
        {
            string Log(Exception e = null)
            {
                const string MSG = "The format of the IoT Hub connection string provided is not valid. "
                                   + "The correct format is: HostName=[hubname];SharedAccessKeyName="
                                   + "[iothubowner or service];SharedAccessKey=[key (or empty if pre-provisioned)]";
                this.log.Error(MSG, e);
                this.diagnosticsLogger.LogServiceError(MSG, e?.Message);
                return MSG;
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidIotHubConnectionStringFormatException(Log());
            }

            var data = Regex.Match(connectionString, REGEX);
            if (!data.Success)
            {
                throw new InvalidIotHubConnectionStringFormatException(Log());
            }

            var host = data.Groups[REGEX_HOSTNAME].Value;
            var keyName = data.Groups[REGEX_KEY_NAME].Value;
            var keyValue = data.Groups[REGEX_KEY_VALUE].Value;

            if (string.IsNullOrEmpty(keyValue))
            {
                if (keyCanBeEmpty)
                {
                    return (host, keyName, "");
                }

                throw new InvalidIotHubConnectionStringFormatException(Log());
            }

            // Validate the key using the SDK which has some stricter logic, e.g. requires a secret
            try
            {
                var newRegistry = this.factory.Resolve<IRegistryManager>();
                newRegistry.ValidateConnectionString(connectionString);
                newRegistry.Dispose();
                return (host, keyName, keyValue);
            }
            catch (Exception e)
            {
                throw new InvalidIotHubConnectionStringFormatException(Log(e));
            }
        }

        // Check if the connection string allows to connect and provides the required access
        public async Task TestAsync(string connectionString, bool keyCanBeEmpty)
        {
            connectionString = connectionString.Trim();

            // The connection string is valid if it's the pre-provisioned IoT Hub
            if (this.IsEmptyOrDefault(connectionString))
            {
                return;
            }

            // If the string includes a secret (i.e. it's not the default connstring), check its functionality
            var data = this.Parse(connectionString, keyCanBeEmpty);
            if (!string.IsNullOrWhiteSpace(data.keyValue))
            {
                this.log.Info("Testing connection string...");
                await this.TestServiceConnectAsync(connectionString);
                await this.TestRegistryReadWriteAsync(connectionString);
            }

            this.log.Debug("The Iot Hub connection string provided is valid.");
        }

        // Check if the connection string grants "ServiceConnect"
        // @see https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-security
        // * access to cloud service-facing communication and monitoring endpoints
        // * receive device-to-cloud messages, send cloud-to-device messages, and retrieve the corresponding delivery acknowledgments
        // * retrieve delivery acknowledgements for file uploads
        // * access twins to update tags and desired properties, retrieve reported properties, and run queries
        private async Task TestServiceConnectAsync(string connectionString)
        {
            try
            {
                var newServiceClient = this.factory.Resolve<IServiceClient>();
                newServiceClient.Init(connectionString);
                await newServiceClient.GetServiceStatisticsAsync();
                newServiceClient.Dispose();
                this.log.Debug("'Service Connect' test passed");
            }
            catch (Exception e)
            {
                const string MSG = "The IoT Hub connection string does not provide 'Service Connect' permission";
                this.log.Error(MSG, e);
                this.diagnosticsLogger.LogServiceError(MSG, e.Message);
                throw new IotHubConnectionException(MSG, e);
            }
        }

        // Check if the connection string grants "RegistryReadWrite"
        // @see https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-security
        // * read and write access to the identity registry
        // * create device or module identity
        // * update device or module identity
        // * retrieve device or module identity by ID
        // * delete device or module identity
        // * list up to 1000 identities
        // * export device identities to Azure blob storage
        // * import device identities from Azure blob storage
        private async Task TestRegistryReadWriteAsync(string connectionString)
        {
            var newRegistry = this.factory.Resolve<IRegistryManager>();
            newRegistry.Init(connectionString);

            string testDeviceId = "test-registry-access-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                await newRegistry.RemoveDeviceAsync(testDeviceId);
                newRegistry.Dispose();
                this.log.Debug("'Registry Read/Write' test passed");
            }
            catch (Microsoft.Azure.Devices.Client.Exceptions.DeviceNotFoundException)
            {
                // Nothing to do
            }
            catch (Microsoft.Azure.Devices.Common.Exceptions.DeviceNotFoundException)
            {
                // Nothing to do
            }
            catch (Exception e)
            {
                const string MSG = "The IoT Hub connection string does not provide 'Registry Read/Write' permission";
                this.log.Error(MSG, () => new { testDeviceId, e });
                this.diagnosticsLogger.LogServiceError(MSG, new { testDeviceId, e.Message });
                throw new IotHubConnectionException(MSG, e);
            }
        }
    }
}
