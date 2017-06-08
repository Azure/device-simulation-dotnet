// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    public interface IServicesConfig
    {
        string DeviceTypesFolder { get; set; }
        string DeviceTypesBehaviorFolder { get; set; }
        string IoTHubManagerApiHost { get; set; }
        int IoTHubManagerApiPort { get; set; }
    }

    public class ServicesConfig : IServicesConfig
    {
        public string DeviceTypesFolder { get; set; }
        public string DeviceTypesBehaviorFolder { get; set; }
        public string IoTHubManagerApiHost { get; set; }
        public int IoTHubManagerApiPort { get; set; }
    }
}
