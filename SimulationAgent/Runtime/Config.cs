// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

// TODO: tests
// TODO: handle errors
// TODO: use JSON?
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
        private const string Application = "device-simulation.";

        /// <summary>Service layer configuration</summary>
        public IServicesConfig ServicesConfig { get; }

        public Config(IConfigData configData)
        {
            this.ServicesConfig = new ServicesConfig
            {
                DeviceTypesFolder = configData.GetString(Application + "device-types-folder"),
                DeviceTypesBehaviorFolder = configData.GetString(Application + "device-types-behavior-folder"),
                IoTHubManagerApiHost = configData.GetString("iothubmanager.webservice.host"),
                IoTHubManagerApiPort = configData.GetInt("iothubmanager.webservice.port")
            };
        }
    }
}
