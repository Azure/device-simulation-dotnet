// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    public interface IConfig
    {
        string HubConnString { get; set; }
    }

    public class Config : IConfig
    {
        public string HubConnString { get; set; }
    }
}
