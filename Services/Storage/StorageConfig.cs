// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage
{
    public class StorageConfig
    {
        private const int DEFAULT_MAX_PENDING_STORAGE_OPERATIONS = 25;
        private const string DEFAULT_STORAGE_TYPE = "documentDb";
        private const int DEFAULT_DOCUMENTDB_THROUGHPUT = 400;

        public string StorageType { get; set; }
        public int MaxPendingOperations { get; set; }
        public string DocumentDbConnString { get; set; }
        public string DocumentDbDatabase { get; set; }
        public string DocumentDbCollection { get; set; }
        public int DocumentDbThroughput { get; set; }
        public int DocumentDbPageSize { get; set; }

        public StorageConfig()
        {
            this.MaxPendingOperations = DEFAULT_MAX_PENDING_STORAGE_OPERATIONS;
            this.StorageType = DEFAULT_STORAGE_TYPE;
            this.DocumentDbThroughput = DEFAULT_DOCUMENTDB_THROUGHPUT;
        }
    }
}
