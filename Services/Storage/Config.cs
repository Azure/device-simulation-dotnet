// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage
{
    public class Config
    {
        private const int DEFAULT_MAX_PENDING_STORAGE_OPERATIONS = 25;
        private const int DEFAULT_COSMOSDBSQL_THROUGHPUT = 400;

        public Type StorageType { get; set; }
        public int MaxPendingOperations { get; set; }

        // Cosmos DB SQL parameters
        public string CosmosDbSqlConnString { get; set; }
        public string CosmosDbSqlDatabase { get; set; }
        public string CosmosDbSqlCollection { get; set; }
        public int CosmosDbSqlThroughput { get; set; }

        // Azure Table Storage parameters
        public string TableStorageConnString { get; set; }
        public string TableStorageTableName { get; set; }

        public Config()
        {
            this.StorageType = Type.Unknown;
            this.MaxPendingOperations = DEFAULT_MAX_PENDING_STORAGE_OPERATIONS;
            this.CosmosDbSqlThroughput = DEFAULT_COSMOSDBSQL_THROUGHPUT;
        }
    }
}
