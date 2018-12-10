// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.CosmosDbSql
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

        // Cosmos DB SQL SDK wrapper and resources
        private ISDKWrapper cosmosDbSql;
        private IDocumentClient cosmosDbSqlClient;

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
            this.cosmosDbSqlClient = null;
            this.cosmosDbSql = null;
        }

        public IEngine Init(Config cfg)
        {
            this.instance.InitOnce();

            this.storageConfig = cfg;
            this.storageName = "CosmosDbSql:" + cfg.CosmosDbSqlDatabase + "/" + cfg.CosmosDbSqlCollection;
            this.cosmosDbSql = this.factory.Resolve<ISDKWrapper>();

            this.instance.InitComplete();

            this.log.Debug("Cosmos DB SQL instance initialized", () => new { this.storageName });

            return this;
        }

        public IDataRecord BuildRecord(string id)
        {
            return new DataRecord { Id = id };
        }

        public IDataRecord BuildRecord(string id, string data)
        {
            var record = new DataRecord { Id = id };
            record.SetData(data);
            return record;
        }

        public async Task<IDataRecord> GetAsync(string id)
        {
            return (await this.RetrieveAsync(id, true)).record;
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return (await this.RetrieveAsync(id, false)).found;
        }

        public async Task<IEnumerable<IDataRecord>> GetAllAsync()
        {
            await this.SetupStorageAsync();

            var result = new List<IDataRecord>();

            try
            {
                this.log.Debug("Fetching all records", () => new { this.storageName });
                IList<DataRecord> storageRecords = this.cosmosDbSql
                    .CreateQuery<DataRecord>(this.cosmosDbSqlClient, this.storageConfig);

                // Delete expired records
                foreach (var record in storageRecords)
                {
                    if (record.IsExpired())
                    {
                        this.log.Debug("Deleting expired record", () => new { this.storageName, record.Id, record.ETag });
                        await this.TryToDeleteExpiredRecord(record.Id);
                    }
                    else
                    {
                        result.Add(record);
                    }
                }

                return result;
            }
            catch (CustomException)
            {
                throw;
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while reading from Cosmos DB SQL", () => new { this.storageName, e });
                throw new ExternalDependencyException(e);
            }
        }

        public async Task<IDataRecord> CreateAsync(IDataRecord input)
        {
            await this.SetupStorageAsync();

            try
            {
                this.log.Debug("Creating new record", () => new { this.storageName, Id = input.GetId() });
                var record = (DataRecord) input;
                record.Touch();
                var response = await this.cosmosDbSql.CreateAsync(this.cosmosDbSqlClient, this.storageConfig, record);
                return this.DocumentToRecord(response?.Resource);
            }
            catch (CustomException)
            {
                throw;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.Conflict)
            {
                this.log.Info("There is already a record with the id specified", () => new { this.storageName, Id = input.GetId() });
                throw new ConflictingResourceException($"There is already a record with id = '{input.GetId()}'.");
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while writing to Cosmos DB SQL",
                    () => new { this.storageName, Id = input.GetId(), e });
                throw new ExternalDependencyException(e);
            }
        }

        public async Task<IDataRecord> UpsertAsync(IDataRecord input)
        {
            return await this.UpsertAsync(input, input.GetETag());
        }

        public async Task<IDataRecord> UpsertAsync(IDataRecord input, string eTag)
        {
            await this.SetupStorageAsync();

            try
            {
                this.log.Debug("Upserting record", () => new { this.storageName, Id = input.GetId(), eTag });
                var record = (DataRecord) input;
                record.Touch();
                var response = await this.cosmosDbSql.UpsertAsync(this.cosmosDbSqlClient, this.storageConfig, record, eTag);
                return this.DocumentToRecord(response?.Resource);
            }
            catch (CustomException)
            {
                throw;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                this.log.Info(
                    "ETag mismatch: the record has been updated by another client.",
                    () => new { this.storageName, Id = input.GetId(), ETag = input.GetETag() });
                throw new ConflictingResourceException("ETag mismatch: the record has been updated by another client.");
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while writing to Cosmos DB SQL",
                    () => new { this.storageName, Id = input.GetId(), eTag, e });
                throw new ExternalDependencyException(e);
            }
        }

        public async Task DeleteAsync(string id)
        {
            await this.SetupStorageAsync();

            try
            {
                this.log.Debug("Deleting record", () => new { this.storageName, id });
                await this.cosmosDbSql.DeleteAsync(this.cosmosDbSqlClient, this.storageConfig, id);
            }
            catch (CustomException)
            {
                throw;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                this.log.Debug("The record requested doesn't exist, nothing to do.", () => new { this.storageName, id });
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while writing to Cosmos DB SQL",
                    () => new { this.storageName, id, e });
                throw new ExternalDependencyException(e);
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
                IDataRecord record = (await this.RetrieveAsync(id, true)).record;

                if (record.IsLockedByOthers(ownerId, ownerType))
                {
                    this.log.Debug("The record is locked by another client",
                        () => new { this.storageName, id, ownerId, ownerType });
                    return false;
                }

                record.Touch();
                record.Lock(ownerId, ownerType, durationSeconds);
                await this.cosmosDbSql.UpsertAsync(this.cosmosDbSqlClient, this.storageConfig, (Resource) record);

                return true;
            }
            catch (CustomException)
            {
                throw;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                this.log.Debug("ETag mismatch: the record has been updated by another client and cannot be locked.",
                    () => new { this.storageName, id, ownerId, ownerType });
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                this.log.Warn("The record doesn't exist and cannot be locked",
                    () => new { this.storageName, id, ownerId, ownerType });
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while writing to Cosmos DB SQL",
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
                IDataRecord record = (await this.RetrieveAsync(id, true)).record;

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
                await this.cosmosDbSql.UpsertAsync(this.cosmosDbSqlClient, this.storageConfig, (Resource) record);
                return true;
            }
            catch (CustomException)
            {
                throw;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                this.log.Debug("ETag mismatch: the record has been updated by another client and cannot be unlocked.",
                    () => new { this.storageName, id, ownerId, ownerType });
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                this.log.Warn("The record doesn't exist and cannot be unlocked",
                    () => new { this.storageName, id, ownerId, ownerType });
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while writing to Cosmos DB SQL",
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
                (this.cosmosDbSqlClient as IDisposable)?.Dispose();
            }

            this.disposedValue = true;
        }

        private async Task SetupStorageAsync()
        {
            this.instance.InitRequired();

            if (this.cosmosDbSqlClient == null)
            {
                this.log.Debug("Getting Cosmos DB SQL Client...", () => new { this.storageConfig.CosmosDbSqlDatabase, this.storageConfig.CosmosDbSqlCollection });
                this.cosmosDbSqlClient = await this.cosmosDbSql.GetClientAsync(this.storageConfig);
                this.log.Debug("Cosmos DB SQL Client ready", () => new { this.storageConfig.CosmosDbSqlDatabase, this.storageConfig.CosmosDbSqlCollection });
            }
        }

        private async Task<(bool found, IDataRecord record)> RetrieveAsync(string id, bool throwIfNotFound)
        {
            await this.SetupStorageAsync();

            try
            {
                this.log.Debug("Fetching record...", () => new { this.storageName, id });
                IResourceResponse<Document> response = await this.cosmosDbSql.ReadAsync(this.cosmosDbSqlClient, this.storageConfig, id);
                this.log.Debug("Record fetched", () => new { this.storageName, id });

                Document document = response?.Resource;
                if (document == null)
                {
                    if (throwIfNotFound) throw new ResourceNotFoundException($"The resource {id} doesn't exist.");
                    return (false, null);
                }

                var record = this.DocumentToRecord(document);
                if (record.IsExpired())
                {
                    this.log.Debug("The record requested has expired, deleting...", () => new { this.storageName, id });
                    await this.TryToDeleteExpiredRecord(id);
                    this.log.Debug("Expired record deleted", () => new { this.storageName, id });

                    if (throwIfNotFound) throw new ResourceNotFoundException($"The record '{id}' doesn't exist.");
                    return (false, null);
                }

                return (true, record);
            }
            catch (CustomException)
            {
                throw;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                this.log.Debug("The record requested doesn't exist.", () => new { this.storageName, id });
                if (throwIfNotFound) throw new ResourceNotFoundException($"The record '{id}' doesn't exist.");
                return (false, null);
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while reading from Cosmos DB SQL",
                    () => new { this.storageName, id, e });
                throw new ExternalDependencyException(e);
            }
        }

        private DataRecord DocumentToRecord(Document doc)
        {
            if (doc == null) return null;

            var record = new DataRecord
            {
                Id = doc.Id,
                Data = doc.GetPropertyValue<string>("Data"),
                ExpirationUtcMsecs = doc.GetPropertyValue<long>("ExpirationUtcMsecs"),
                LastModifiedUtcMsecs = doc.GetPropertyValue<long>("LastModifiedUtcMsecs"),
                LockOwnerId = doc.GetPropertyValue<string>("LockOwnerId"),
                LockOwnerType = doc.GetPropertyValue<string>("LockOwnerType"),
                LockExpirationUtcMsecs = doc.GetPropertyValue<long>("LockExpirationUtcMsecs"),
            };
            record.SetETag(doc.ETag);

            return record;
        }

        private async Task TryToDeleteExpiredRecord(string id)
        {
            try
            {
                await this.cosmosDbSql.DeleteAsync(this.cosmosDbSqlClient, this.storageConfig, id);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                // Not an error, deletions are idempotent
                this.log.Debug("The record requested doesn't exist, nothing to do", () => new { this.storageName, id });
            }
            catch (Exception e)
            {
                // Log and do not throw, we're just trying to delete and will retry automatically later
                this.log.Warn("Unexpected error while writing to Cosmos DB SQL", () => new { this.storageName, id, e });
            }
        }
    }
}
