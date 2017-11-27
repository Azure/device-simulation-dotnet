// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Auth;

// TODO: tests
// TODO: handle errors
// TODO: use binding
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime
{
    public interface IConfig
    {
        // Web service listening port
        int Port { get; }

        // Service layer configuration
        IServicesConfig ServicesConfig { get; }

        // Client authentication and authorization configuration
        IClientAuthConfig ClientAuthConfig { get; }
    }

    /// <summary>Web service configuration</summary>
    public class Config : IConfig
    {
        private const string APPLICATION_KEY = "DeviceSimulationService:";
        private const string PORT_KEY = APPLICATION_KEY + "webservice_port";
        private const string DEVICE_MODELS_FOLDER_KEY = APPLICATION_KEY + "device_models_folder";
        private const string DEVICE_MODELS_SCRIPTS_FOLDER_KEY = APPLICATION_KEY + "device_models_scripts_folder";
        private const string IOT_HUB_FOLDER_KEY = APPLICATION_KEY + "iot_hub_folder";
        private const string IOTHUB_CONNSTRING_KEY = APPLICATION_KEY + "iothub_connstring";

        private const string TWIN_READ_WRITE_ENABLED_KEY = APPLICATION_KEY + "twin_read_write_enabled";

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

        private const string CLIENT_AUTH_KEY = APPLICATION_KEY + "ClientAuth:";
        private const string CORS_WHITELIST_KEY = CLIENT_AUTH_KEY + "cors_whitelist";
        private const string AUTH_TYPE_KEY = CLIENT_AUTH_KEY + "auth_type";
        private const string AUTH_REQUIRED_KEY = CLIENT_AUTH_KEY + "auth_required";

        private const string JWT_KEY = APPLICATION_KEY + "ClientAuth:JWT:";
        private const string JWT_ALGOS_KEY = JWT_KEY + "allowed_algorithms";
        private const string JWT_ISSUER_KEY = JWT_KEY + "issuer";
        private const string JWT_AUDIENCE_KEY = JWT_KEY + "audience";
        private const string JWT_CLOCK_SKEW_KEY = JWT_KEY + "clock_skew_seconds";

        public int Port { get; }
        public IServicesConfig ServicesConfig { get; }
        public IClientAuthConfig ClientAuthConfig { get; }

        public Config(IConfigData configData)
        {
            this.Port = configData.GetInt(PORT_KEY);

            var connstring = configData.GetString(IOTHUB_CONNSTRING_KEY);
            if (connstring.ToLowerInvariant().Contains("azure iot hub"))
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
                IoTHubFolder = MapRelativePath(configData.GetString(IOT_HUB_FOLDER_KEY)),
                IoTHubConnString = connstring,
                StorageAdapterApiUrl = configData.GetString(STORAGE_ADAPTER_API_URL_KEY),
                StorageAdapterApiTimeout = configData.GetInt(STORAGE_ADAPTER_API_TIMEOUT_KEY),
                RateLimiting = limitsConf,
                TwinReadWriteEnabled = configData.GetBool(TWIN_READ_WRITE_ENABLED_KEY, true)
            };

            this.ClientAuthConfig = new ClientAuthConfig
            {
                // By default CORS is disabled
                CorsWhitelist = configData.GetString(CORS_WHITELIST_KEY, string.Empty),
                // By default Auth is required
                AuthRequired = configData.GetBool(AUTH_REQUIRED_KEY, true),
                // By default auth type is JWT
                AuthType = configData.GetString(AUTH_TYPE_KEY, "JWT"),
                // By default the only trusted algorithms are RS256, RS384, RS512
                JwtAllowedAlgos = configData.GetString(JWT_ALGOS_KEY, "RS256,RS384,RS512").Split(','),
                JwtIssuer = configData.GetString(JWT_ISSUER_KEY, String.Empty),
                JwtAudience = configData.GetString(JWT_AUDIENCE_KEY, String.Empty),
                // By default the allowed clock skew is 2 minutes
                JwtClockSkew = TimeSpan.FromSeconds(configData.GetInt(JWT_CLOCK_SKEW_KEY, 120)),
            };
        }

        private static string MapRelativePath(string path)
        {
            if (path.StartsWith(".")) return AppContext.BaseDirectory + Path.DirectorySeparatorChar + path;
            return path;
        }
    }
}
