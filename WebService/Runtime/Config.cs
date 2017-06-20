// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting.Internal;
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

        /// <summary>Service layer configuration</summary>
        IServicesConfig ServicesConfig { get; }
    }

    /// <summary>Web service configuration</summary>
    public class Config : IConfig
    {
        private const string Application = "devicesimulation:";

        /// <summary>Web service listening port</summary>
        public int Port { get; }

        /// <summary>Service layer configuration</summary>
        public IServicesConfig ServicesConfig { get; }

        public Config(IConfigData configData)
        {
            this.Port = configData.GetInt(Application + "webservice_port");

            this.ServicesConfig = new ServicesConfig
            {
                DeviceTypesFolder = MapRelativePath(configData.GetString(Application + "device_types_folder")),
                DeviceTypesBehaviorFolder = MapRelativePath(configData.GetString(Application + "device_types_behavior_folder")),
                IoTHubManagerApiHost = configData.GetString("iothubmanager:webservice_host"),
                IoTHubManagerApiPort = configData.GetInt("iothubmanager:webservice_port")
            };
        }

        private static string MapRelativePath(string path)
        {
            if (path.StartsWith(".")) return AppContext.BaseDirectory + Path.DirectorySeparatorChar + path;
            return path;
        }
    }
}
