// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

// TODO: tests
// TODO: handle errors
// TODO: use binding
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Runtime
{
    public interface IConfig
    {
        /// <summary>Service layer configuration</summary>
        IServicesConfig ServicesConfig { get; }
    }

    /// <summary>Web service configuration</summary>
    public class Config : IConfig
    {
        private const string ApplicationKey = "devicesimulation:";
        private const string DeviceTypesFolderKey = ApplicationKey + "device_types_folder";
        private const string DeviceTypesScriptsFolderKey = ApplicationKey + "device_types_scripts_folder";
        private const string IoTHubConnStringKey = ApplicationKey + "iothub_connstring";

        private const string StorageAdapterKey = "storageadapter:";
        private const string StorageAdapterApiUrlKey = StorageAdapterKey + "webservice_url";
        private const string StorageAdapterApiTimeoutKey = StorageAdapterKey + "webservice_timeout";

        /// <summary>Service layer configuration</summary>
        public IServicesConfig ServicesConfig { get; }

        public Config(IConfigData configData)
        {
            var connstring = configData.GetString(IoTHubConnStringKey);
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
                DeviceTypesFolder = MapRelativePath(configData.GetString(DeviceTypesFolderKey)),
                DeviceTypesScriptsFolder = MapRelativePath(configData.GetString(DeviceTypesScriptsFolderKey)),
                IoTHubConnString = connstring,
                StorageAdapterApiUrl = configData.GetString(StorageAdapterApiUrlKey),
                StorageAdapterApiTimeout = configData.GetInt(StorageAdapterApiTimeoutKey)
            };
        }

        private static string MapRelativePath(string path)
        {
            if (path.StartsWith(".")) return AppContext.BaseDirectory + Path.DirectorySeparatorChar + path;
            return path;
        }
    }
}
