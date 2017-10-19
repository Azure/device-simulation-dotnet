// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Runtime
{
    public interface IConfig
    {
        // Service layer configuration
        IServicesConfig ServicesConfig { get; }
    }

    /// <summary>Simulation agent configuration</summary>
    public class Config : IConfig
    {
        private const string APPLICATION_KEY = "DeviceSimulationService:";
        private const string DEVICE_MODELS_FOLDER_KEY = APPLICATION_KEY + "device_models_folder";
        private const string DEVICE_MODELS_SCRIPTS_FOLDER_KEY = APPLICATION_KEY + "device_models_scripts_folder";
        private const string IOTHUB_CONNSTRING_KEY = APPLICATION_KEY + "iothub_connstring";

        private const string IOTHUB_LIMITS_KEY = APPLICATION_KEY + "RateLimits:";
        private const string CONNECTIONS_FREQUENCY_LIMIT_KEY = IOTHUB_LIMITS_KEY + "device_connections_per_second";
        private const string REGISTRYOPS_FREQUENCY_LIMIT_KEY = IOTHUB_LIMITS_KEY + "registry_operations_per_minute";
        private const string DEVICE_MESSAGES_FREQUENCY_LIMIT_KEY = IOTHUB_LIMITS_KEY + "device_to_cloud_messages_per_second";
        private const string DEVICE_MESSAGES_DAILY_LIMIT_KEY = IOTHUB_LIMITS_KEY + "device_to_cloud_messages_per_day";
        private const string TWIN_READS_FREQUENCY_LIMIT_KEY = IOTHUB_LIMITS_KEY + "twin_reads_per_second";
        private const string TWIN_WRITES_FREQUENCY_LIMIT_KEY = IOTHUB_LIMITS_KEY + "twin_writes_per_second";

        private const string STORAGE_ADAPTER_KEY = "StorageAdapterService:";
        private const string STORAGE_ADAPTER_API_URL_KEY = STORAGE_ADAPTER_KEY + "webservice_url";
        private const string STORAGE_ADAPTER_API_TIMEOUT_KEY = STORAGE_ADAPTER_KEY + "webservice_timeout";

        public IServicesConfig ServicesConfig { get; }

        public Config(IConfigData configData)
        {
            var connstring = configData.GetString(IOTHUB_CONNSTRING_KEY);
            if (connstring.ToLowerInvariant().Contains("your azure iot hub"))
            {
                // In order to connect to Azure IoT Hub, the service requires a connection
                // string. The value can be found in the Azure Portal. For more information see
                // https://docs.microsoft.com/azure/iot-hub/iot-hub-csharp-csharp-getstarted
                // to find the connection string value.
                // The connection string can be stored in the 'appsettings.ini' configuration
                // file, or in the PCS_IOTHUB_CONNSTRING environment variable. When
                // working with VisualStudio, the environment variable can be set in the
                // WebService project settings, under the "Debug" tab.
                throw new Exception("The service configuration is incomplete. " +
                                    "Please provide your Azure IoT Hub connection string. " +
                                    "For more information, see the environment variables " +
                                    "used in project properties and the 'iothub_connstring' " +
                                    "value in the 'appsettings.ini' configuration file.");
            }

            var limitsConf = new RateLimitingConfiguration
            {
                ConnectionsPerSecond = configData.GetInt(CONNECTIONS_FREQUENCY_LIMIT_KEY, 50),
                RegistryOperationsPerMinute = configData.GetInt(REGISTRYOPS_FREQUENCY_LIMIT_KEY, 50),
                DeviceMessagesPerSecond = configData.GetInt(DEVICE_MESSAGES_FREQUENCY_LIMIT_KEY, 50),
                DeviceMessagesPerDay = configData.GetInt(DEVICE_MESSAGES_DAILY_LIMIT_KEY, 8000),
                TwinReadsPerSecond = configData.GetInt(TWIN_READS_FREQUENCY_LIMIT_KEY, 5),
                TwinWritesPerSecond = configData.GetInt(TWIN_WRITES_FREQUENCY_LIMIT_KEY, 5)
            };

            this.ServicesConfig = new ServicesConfig
            {
                DeviceModelsFolder = MapRelativePath(configData.GetString(DEVICE_MODELS_FOLDER_KEY)),
                DeviceModelsScriptsFolder = MapRelativePath(configData.GetString(DEVICE_MODELS_SCRIPTS_FOLDER_KEY)),
                IoTHubConnString = connstring,
                StorageAdapterApiUrl = configData.GetString(STORAGE_ADAPTER_API_URL_KEY),
                StorageAdapterApiTimeout = configData.GetInt(STORAGE_ADAPTER_API_TIMEOUT_KEY),
                RateLimiting = limitsConf
            };
        }

        private static string MapRelativePath(string path)
        {
            if (path.StartsWith(".")) return AppContext.BaseDirectory + Path.DirectorySeparatorChar + path;
            return path;
        }
    }
}
