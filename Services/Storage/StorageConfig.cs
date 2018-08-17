// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage
{
    public class StorageConfig
    {
        public string StorageType { get; set; }
        public string DocumentDbConnString { get; set; }
        public string DocumentDbDatabase { get; set; }
        public string DocumentDbCollection { get; set; }
        public int DocumentDbRUs { get; set; }
    }
}