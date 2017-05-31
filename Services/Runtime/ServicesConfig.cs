// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    public interface IServicesConfig
    {
        string DeviceTypesFolder { get; set; }
        string DeviceTypesBehaviorFolder { get; set; }
    }

    public class ServicesConfig : IServicesConfig
    {
        public string DeviceTypesFolder { get; set; }
        public string DeviceTypesBehaviorFolder { get; set; }
    }
}
