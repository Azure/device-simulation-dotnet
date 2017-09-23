// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

// TODO: tests
// TODO: handle errors
// TODO: use binding
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime
{
    public interface IConfig
    {
        /// <summary>Web service listening port</summary>
        int Port { get; }

        /// <summary>CORS whitelist, in form { 'origins': [], 'methods': [], 'headers': [] }</summary>
        string CorsWhitelist { get; }

        /// <summary>Service layer configuration</summary>
        IServicesConfig ServicesConfig { get; }
    }

    /// <summary>Web service configuration</summary>
    public class Config : IConfig
    {
        private const string APPLICATION_KEY = "devicesimulation:";
        private const string PORT_KEY = APPLICATION_KEY + "webservice_port";
        private const string DEVICE_MODELS_FOLDER_KEY = APPLICATION_KEY + "device_models_folder";
        private const string DEVICE_MODELS_SCRIPTS_FOLDER_KEY = APPLICATION_KEY + "device_models_scripts_folder";
        private const string IO_T_HUB_CONN_STRING_KEY = APPLICATION_KEY + "iothub_connstring";
        private const string CORS_WHITELIST_KEY = APPLICATION_KEY + "cors_whitelist";

        private const string STORAGE_ADAPTER_KEY = "storageadapter:";
        private const string STORAGE_ADAPTER_API_URL_KEY = STORAGE_ADAPTER_KEY + "webservice_url";
        private const string STORAGE_ADAPTER_API_TIMEOUT_KEY = STORAGE_ADAPTER_KEY + "webservice_timeout";

        /// <summary>Web service listening port</summary>
        public int Port { get; }

        /// <summary>CORS whitelist, in form { 'origins': [], 'methods': [], 'headers': [] }</summary>
        public string CorsWhitelist { get; }

        /// <summary>Service layer configuration</summary>
        public IServicesConfig ServicesConfig { get; }

        public Config(IConfigData configData)
        {
            this.Port = configData.GetInt(PORT_KEY);
            this.CorsWhitelist = configData.GetString(CORS_WHITELIST_KEY);

            var connstring = configData.GetString(IO_T_HUB_CONN_STRING_KEY);
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

            this.ServicesConfig = new ServicesConfig
            {
                DeviceModelsFolder = MapRelativePath(configData.GetString(DEVICE_MODELS_FOLDER_KEY)),
                DeviceModelsScriptsFolder = MapRelativePath(configData.GetString(DEVICE_MODELS_SCRIPTS_FOLDER_KEY)),
                IoTHubConnString = connstring,
                StorageAdapterApiUrl = configData.GetString(STORAGE_ADAPTER_API_URL_KEY),
                StorageAdapterApiTimeout = configData.GetInt(STORAGE_ADAPTER_API_TIMEOUT_KEY)
            };
        }

        private static string MapRelativePath(string path)
        {
            if (path.StartsWith(".")) return AppContext.BaseDirectory + Path.DirectorySeparatorChar + path;
            return path;
        }
    }
}
