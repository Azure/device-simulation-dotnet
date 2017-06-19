﻿// Copyright (c) Microsoft. All rights reserved.

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
        private const string Application = "devicesimulation:";

        /// <summary>Service layer configuration</summary>
        public IServicesConfig ServicesConfig { get; }

        public Config(IConfigData configData)
        {
            this.ServicesConfig = new ServicesConfig
            {
                DeviceTypesFolder = configData.GetString(Application + "device_types_folder"),
                DeviceTypesBehaviorFolder = configData.GetString(Application + "device_types_behavior_folder"),
                IoTHubManagerApiHost = configData.GetString("iothubmanager:webservice_host"),
                IoTHubManagerApiPort = configData.GetInt("iothubmanager:webservice_port")
            };
        }
    }
}
