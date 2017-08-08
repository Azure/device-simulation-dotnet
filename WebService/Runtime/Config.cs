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
        private const string ApplicationKey = "devicesimulation:";
        private const string PortKey = ApplicationKey + "webservice_port";
        private const string DeviceTypesFolderKey = ApplicationKey + "device_types_folder";
        private const string DeviceTypesScriptsFolderKey = ApplicationKey + "device_types_scripts_folder";
        private const string CorsWhitelistKey = ApplicationKey + "cors_whitelist";

        private const string IoTHubManagerKey = "iothubmanager:";
        private const string IoTHubManagerApiUrlKey = IoTHubManagerKey + "webservice_url";
        private const string IoTHubManagerApiTimeoutKey = IoTHubManagerKey + "webservice_timeout";

        /// <summary>Web service listening port</summary>
        public int Port { get; }

        /// <summary>CORS whitelist, in form { 'origins': [], 'methods': [], 'headers': [] }</summary>
        public string CorsWhitelist { get; }

        /// <summary>Service layer configuration</summary>
        public IServicesConfig ServicesConfig { get; }

        public Config(IConfigData configData)
        {
            this.Port = configData.GetInt(PortKey);
            this.CorsWhitelist = configData.GetString(CorsWhitelistKey);

            this.ServicesConfig = new ServicesConfig
            {
                DeviceTypesFolder = MapRelativePath(configData.GetString(DeviceTypesFolderKey)),
                DeviceTypesScriptsFolder = MapRelativePath(configData.GetString(DeviceTypesScriptsFolderKey)),
                IoTHubManagerApiUrl = configData.GetString(IoTHubManagerApiUrlKey),
                IoTHubManagerApiTimeout = configData.GetInt(IoTHubManagerApiTimeoutKey)
            };
        }

        private static string MapRelativePath(string path)
        {
            if (path.StartsWith(".")) return AppContext.BaseDirectory + Path.DirectorySeparatorChar + path;
            return path;
        }
    }
}
