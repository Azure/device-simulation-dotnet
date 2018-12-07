// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.TableStorage
{
    public interface ISDKWrapper
    {
        CloudTableClient CreateCloudTableClient(Config config);
        Task<CloudTable> GetTableReferenceAsync(CloudTableClient tableStorageClient, Config storageConfig);
        Task<TableResult> RetrieveAsync(CloudTable table, string id);
        Task<TableResult> ExecuteAsync(CloudTable table, TableOperation operation);

        Task<(IList<DataRecord> records, TableContinuationToken continuationToken)>
            ExecuteQuerySegmentedAsync(
                CloudTable table,
                TableQuery<DataRecord> query,
                TableContinuationToken token);
    }

    public class SDKWrapper : ISDKWrapper
    {
        public const string PK_FIELD = "PartitionKey";

        private readonly ILogger log;

        public SDKWrapper(ILogger logger)
        {
            this.log = logger;
        }

        public CloudTableClient CreateCloudTableClient(Config config)
        {
            CloudStorageAccount storageAccount = this.GetStorageAccount(config.TableStorageConnString);
            return storageAccount.CreateCloudTableClient();
        }

        public async Task<CloudTable> GetTableReferenceAsync(CloudTableClient tableClient, Config storageConfig)
        {
            var table = tableClient.GetTableReference(storageConfig.TableStorageTableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        public async Task<TableResult> RetrieveAsync(CloudTable table, string id)
        {
            var operation = TableOperation.Retrieve<DataRecord>(DataRecord.FIXED_PKEY, id);
            return await table.ExecuteAsync(operation);
        }

        public async Task<TableResult> ExecuteAsync(CloudTable table, TableOperation operation)
        {
            return await table.ExecuteAsync(operation);
        }

        public async Task<(IList<DataRecord> records, TableContinuationToken continuationToken)>
            ExecuteQuerySegmentedAsync(
                CloudTable table,
                TableQuery<DataRecord> query,
                TableContinuationToken token)
        {
            TableQuerySegment<DataRecord> result = await table.ExecuteQuerySegmentedAsync(query, token);
            return (result.Results, result.ContinuationToken);
        }

        private CloudStorageAccount GetStorageAccount(string storageConnectionString)
        {
            try
            {
                return CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (FormatException e)
            {
                this.log.Error("Invalid storage account information provided, verify that the AccountName and AccountKey are valid", e);
                throw;
            }
            catch (ArgumentException e)
            {
                this.log.Error("Invalid storage account information provided, verify that the AccountName and AccountKey are valid", e);
                throw;
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while parsing generating the storage account from a connection string", e);
                throw;
            }
        }
    }
}
