// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    public interface IServicesConfig
    {
        string DataFolder { get; set; }
    }

    public class ServicesConfig : IServicesConfig
    {
        public string DataFolder { get; set; }
    }
}
