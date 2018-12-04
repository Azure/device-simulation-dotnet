// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.TableStorage
{
    public class Engine : IEngine, IDisposable
    {
        private readonly ILogger log;
        private readonly IInstance instance;
        private readonly IFactory factory;

        private Config storageConfig;

        // String used only for logging
        private string storageName;

        private bool disposedValue;

        // Azure Table Storage SDK wrapper and resources
        private ISDKWrapper tableStorage;
        private CloudTableClient tableStorageClient;
        private CloudTable tableStorageTable;

        public Engine(
            IFactory factory,
            ILogger logger,
            IInstance instance)
        {
            this.log = logger;
            this.instance = instance;
            this.factory = factory;

            this.disposedValue = false;

            this.storageConfig = null;
            this.tableStorage = null;
        }

        public IEngine Init(Config cfg)
        {
            this.instance.InitOnce();

            this.storageConfig = cfg;
            this.storageName = "TableStorage:" + cfg.TableStorageTableName;
            this.tableStorage = this.factory.Resolve<ISDKWrapper>();

            this.instance.InitComplete();

            return this;
        }

        public IDataRecord BuildRecord(string id)
        {
            return new DataRecord(id);
        }

        public IDataRecord BuildRecord(string id, string data)
        {
            var record = new DataRecord(id);
            record.SetData(data);
            return record;
        }

        public async Task<IDataRecord> GetAsync(string id)
        {
            await this.SetupStorageAsync();

            this.log.Debug("Fetching record...", () => new { this.storageName, id });
            TableResult response = await this.tableStorage.RetrieveAsync(this.tableStorageTable, id);
            this.log.Debug("Record fetched", () => new { this.storageName, id });

            if (response.HttpStatusCode == (int) HttpStatusCode.NotFound)
            {
                throw new ResourceNotFoundException($"The resource {id} doesn't exist.");
            }

            if (response.HttpStatusCode != (int) HttpStatusCode.OK)
            {
                this.log.Error("Unexpected error while retrieving the record from table storage", () =>
                    new { this.storageName, id, response.HttpStatusCode, response.Result });
                throw new ExternalDependencyException(
                    "Unexpected response from table storage (StatusCode = " + response.HttpStatusCode + ")");
            }

            var record = response.Result as DataRecord;

            if (record == null)
            {
                throw new ResourceNotFoundException($"The resource {id} doesn't exist.");
            }

            if (record.IsExpired())
            {
                this.log.Debug("The resource requested has expired, deleting...", () => new { this.storageName, id });
                await this.TryToDeleteExpiredRecord(id);
                this.log.Debug("Expired resource deleted", () => new { this.storageName, id });

                throw new ResourceNotFoundException($"The resource {id} doesn't exist.");
            }

            return record;
        }

        public async Task<bool> ExistsAsync(string id)
        {
            await this.SetupStorageAsync();

            TableResult response = await this.tableStorage.RetrieveAsync(this.tableStorageTable, id);
            var record = response?.Result as DataRecord;

            if (record == null)
            {
                return false;
            }

            if (record.IsExpired())
            {
                this.log.Info("The resource requested has expired.", () => new { this.storageName, id });
                await this.TryToDeleteExpiredRecord(id);
                return false;
            }

            return true;
        }

        public async Task<IEnumerable<IDataRecord>> GetAllAsync()
        {
            await this.SetupStorageAsync();

            var result = new List<IDataRecord>();

            try
            {
                string query = TableQuery.GenerateFilterCondition(
                    SDKWrapper.PK_FIELD, QueryComparisons.Equal, DataRecord.FIXED_PKEY);
                var partitionScanQuery = new TableQuery<DataRecord>().Where(query);

                // Page through the results
                TableContinuationToken token = null;
                do
                {
                    var segment = await this.tableStorage.ExecuteQuerySegmentedAsync(
                        this.tableStorageTable, partitionScanQuery, token);
                    token = segment.ContinuationToken;
                    foreach (DataRecord entity in segment)
                    {
                        if (!entity.IsExpired())
                        {
                            result.Add(entity);
                        }
                        else
                        {
                            this.log.Debug("Deleting expired resource", () => new { this.storageName, Id = entity.GetId(), ETag = entity.GetETag() });
                            await this.TryToDeleteExpiredRecord(entity.GetId());
                        }
                    }
                } while (token != null);

                return result;
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while retrieving the list of resources", () => new { this.storageName, e });
                throw;
            }
        }

        public async Task<IDataRecord> CreateAsync(IDataRecord input)
        {
            return await this.UpsertAsync(input, "");
        }

        public async Task<IDataRecord> UpsertAsync(IDataRecord input)
        {
            return await this.UpsertAsync(input, input.GetETag());
        }

        public async Task<IDataRecord> UpsertAsync(IDataRecord input, string eTag)
        {
            await this.SetupStorageAsync();

            var entity = (DataRecord) input;
            entity.ETag = eTag;
            entity.Touch();

            var operation = TableOperation.InsertOrMerge(entity);
            TableResult response = await this.tableStorage.ExecuteAsync(this.tableStorageTable, operation);

            switch (response.HttpStatusCode)
            {
                case (int) HttpStatusCode.Conflict:
                    this.log.Info("There is already a resource with the id specified.",
                        () => new { this.storageName, Id = input.GetId() });
                    throw new ConflictingResourceException($"There is already a resource with id = '{input.GetId()}'.");

                case (int) HttpStatusCode.PreconditionFailed:
                    this.log.Info(
                        "E-Tag mismatch: the resource has been updated by another client.",
                        () => new { this.storageName, Id = input.GetId(), ETag = input.GetETag() });
                    throw new ConflictingResourceException("E-Tag mismatch: the resource has been updated by another client.");
            }

            if (response.HttpStatusCode > 299)
            {
                this.log.Error("Unexpected error",
                    () => new { this.storageName, Id = input.GetId(), ETag = input.GetETag(), response });

                throw new ExternalDependencyException("Table storage request failed");
            }

            return (DataRecord) response.Result;
        }

        public async Task DeleteAsync(string id)
        {
            await this.SetupStorageAsync();

            var entity = new DataRecord(id);

            var operation = TableOperation.Delete(entity);
            TableResult response = await this.tableStorage.ExecuteAsync(this.tableStorageTable, operation);

            if (response.HttpStatusCode == (int) HttpStatusCode.NotFound)
            {
                this.log.Debug("The resource requested doesn't exist, nothing to do.", () => new { this.storageName, id });
                return;
            }

            if (response.HttpStatusCode > 299)
            {
                this.log.Error("Unexpected error",
                    () => new { this.storageName, id, response });
                throw new ExternalDependencyException("Table storage request failed");
            }
        }

        public async Task DeleteMultiAsync(List<string> ids)
        {
            await this.SetupStorageAsync();

            var tasks = new List<Task>();
            foreach (var id in ids)
            {
                tasks.Add(this.DeleteAsync(id));
                if (tasks.Count < this.storageConfig.MaxPendingOperations) continue;

                await Task.WhenAll(tasks);
                tasks.Clear();
            }

            if (tasks.Count > 0) await Task.WhenAll(tasks);
        }

        public async Task<bool> TryToLockAsync(
            string id,
            string ownerId,
            string ownerType,
            int durationSeconds)
        {
            await this.SetupStorageAsync();

            try
            {
                this.log.Debug("Trying to obtain lock for record", () => new { ownerType });

                // Note: this can throw ResourceNotFoundException
                IDataRecord record = await this.GetAsync(id);

                if (record.IsLockedByOthers(ownerId, ownerType))
                {
                    this.log.Debug("The resource is locked by another client",
                        () => new { this.storageName, id, ownerId, ownerType });
                    return false;
                }

                record.Touch();
                record.Lock(ownerId, ownerType, durationSeconds);

                var entity = (DataRecord) record;
                var operation = TableOperation.InsertOrMerge(entity);
                TableResult response = await this.tableStorage.ExecuteAsync(this.tableStorageTable, operation);

                switch (response.HttpStatusCode)
                {
                    case (int) HttpStatusCode.PreconditionFailed:
                        this.log.Info(
                            "E-Tag mismatch: the resource has been updated by another client.",
                            () => new { this.storageName, Id = record.GetId(), ETag = record.GetETag() });
                        throw new ConflictingResourceException("E-Tag mismatch: the resource has been updated by another client.");
                }

                if (response.HttpStatusCode > 299)
                {
                    this.log.Error("Unexpected error",
                        () => new { this.storageName, Id = record.GetId(), ETag = record.GetETag(), response });

                    throw new ExternalDependencyException("Table storage request failed");
                }

                return true;
            }
            catch (ResourceNotFoundException)
            {
                throw;
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while writing to the storage",
                    () => new { this.storageName, id, ownerId, ownerType, lockDurationSecs = durationSeconds, e });
            }

            return false;
        }

        public async Task<bool> TryToUnlockAsync(string id, string ownerId, string ownerType)
        {
            await this.SetupStorageAsync();

            try
            {
                // Note: this can throw ResourceNotFoundException
                IDataRecord record = await this.GetAsync(id);

                // Nothing to do
                if (!record.IsLocked()) return true;

                if (!record.CanUnlock(ownerId, ownerType))
                {
                    this.log.Debug("The resource is locked by another client and cannot be unlocked by this client",
                        () => new { this.storageName, id, ownerId, ownerType });
                    return false;
                }

                record.Touch();
                record.Unlock(ownerId, ownerType);

                var entity = (DataRecord) record;
                var operation = TableOperation.InsertOrMerge(entity);
                TableResult response = await this.tableStorage.ExecuteAsync(this.tableStorageTable, operation);

                switch (response.HttpStatusCode)
                {
                    case (int) HttpStatusCode.PreconditionFailed:
                        this.log.Info(
                            "E-Tag mismatch: the resource has been updated by another client.",
                            () => new { this.storageName, Id = record.GetId(), ETag = record.GetETag() });
                        throw new ConflictingResourceException("E-Tag mismatch: the resource has been updated by another client.");
                }

                if (response.HttpStatusCode > 299)
                {
                    this.log.Error("Unexpected error",
                        () => new { this.storageName, Id = record.GetId(), ETag = record.GetETag(), response });

                    throw new ExternalDependencyException("Table storage request failed");
                }

                return true;
            }
            catch (ResourceNotFoundException)
            {
                throw;
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while writing to the storage",
                    () => new { this.storageName, id, ownerId, ownerType, e });
            }

            return false;
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposedValue && disposing)
            {
                this.tableStorageClient = null;
                this.tableStorageTable = null;
            }

            this.disposedValue = true;
        }

        private async Task SetupStorageAsync()
        {
            this.instance.InitRequired();

            if (this.tableStorageTable == null)
            {
                this.log.Debug("Getting Table Storage Client...", () => new { this.storageConfig.TableStorageTableName });
                this.tableStorageClient = this.tableStorage.CreateCloudTableClient(this.storageConfig);
                this.tableStorageTable = await this.tableStorage.GetTableReferenceAsync(this.tableStorageClient, this.storageConfig);
                this.log.Debug("Table Storage Client ready", () => new { this.storageConfig.TableStorageTableName });
            }
        }

        private async Task TryToDeleteExpiredRecord(string id)
        {
            var entity = new DataRecord(id);

            var operation = TableOperation.Delete(entity);
            TableResult response = await this.tableStorage.ExecuteAsync(this.tableStorageTable, operation);

            if (response.HttpStatusCode == (int) HttpStatusCode.NotFound)
            {
                this.log.Debug("The resource requested doesn't exist, nothing to do.", () => new { this.storageName, id });
                return;
            }

            if (response.HttpStatusCode > 299)
            {
                this.log.Warn("Unable to delete the record due to an unexpected error.", () => new { this.storageName, id, response });
            }
        }
    }
}
