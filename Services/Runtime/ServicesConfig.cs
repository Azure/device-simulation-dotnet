﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    public interface IServicesConfig
    {
        string HubConnString { get; set; }
        string DataFolder { get; set; }
    }

    public class ServicesConfig : IServicesConfig
    {
        public string HubConnString { get; set; }
        public string DataFolder { get; set; }
    }
}
