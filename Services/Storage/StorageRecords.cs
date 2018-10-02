// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.DocumentDb;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage
{
    public interface IStorageRecords
    {
        IStorageRecords Init(StorageConfig config);
        Task<StorageRecord> GetAsync(string id);
        Task<bool> ExistsAsync(string id);
        Task<IEnumerable<StorageRecord>> GetAllAsync();
        Task<StorageRecord> CreateAsync(StorageRecord input);
        Task<StorageRecord> UpsertAsync(StorageRecord input);
        Task<StorageRecord> UpsertAsync(StorageRecord input, string eTag);
        Task DeleteAsync(string id);
        Task DeleteMultiAsync(List<string> ids);
        Task<bool> TryToLockAsync(string id, string ownerId, string ownerType, int durationSeconds);
        Task<bool> TryToUnlockAsync(string id, string ownerId, string ownerType);
    }

    public class StorageRecords : IStorageRecords, IDisposable
    {
        private readonly ILogger log;
        private readonly IInstance instance;
        private readonly IDocumentDbWrapper docDb;
        private StorageConfig storageConfig;

        private IDocumentClient client;
        private bool disposedValue;
        private string storageName;

        public StorageRecords(
            IDocumentDbWrapper docDb,
            ILogger logger,
            IInstance instance)
        {
            this.log = logger;
            this.instance = instance;
            this.docDb = docDb;
            this.disposedValue = false;
            this.storageConfig = null;
            this.client = null;
        }

        public IStorageRecords Init(StorageConfig cfg)
        {
            this.instance.InitOnce();

            this.storageConfig = cfg;

            // Used only for logging
            this.storageName = cfg.DocumentDbDatabase + "/" + cfg.DocumentDbCollection;

            this.instance.InitComplete();

            return this;
        }

        public async Task<StorageRecord> GetAsync(string id)
        {
            await this.SetupStorageAsync();

            try
            {
                this.log.Debug("Fetching record...", () => new {this.storageName, id});
                var response = await this.docDb.ReadAsync(this.client, this.storageConfig, id);
                this.log.Debug("Record fetched", () => new { this.storageName, id });

                var record = StorageRecord.FromDocumentDb(response?.Resource);

                if (record.IsExpired())
                {
                    this.log.Debug("The resource requested has expired, deleting...", () => new { this.storageName, id });
                    await this.TryToDeleteExpiredRecord(id);
                    this.log.Debug("Expired resource deleted", () => new { this.storageName, id });

                    throw new ResourceNotFoundException($"The resource {id} doesn't exist.");
                }

                return record;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                this.log.Debug("The resource requested doesn't exist.", () => new{this.storageName, id});
                throw new ResourceNotFoundException($"The resource {id} doesn't exist.");
            }
        }
        public async Task<bool> ExistsAsync(string id)
        {
            await this.SetupStorageAsync();

            try
            {
                var response = await this.docDb.ReadAsync(this.client, this.storageConfig, id);
                var record = StorageRecord.FromDocumentDb(response?.Resource);

                if (record.IsExpired())
                {
                    this.log.Info("The resource requested has expired.", () => new { this.storageName, id });
                    await this.TryToDeleteExpiredRecord(id);
                    return false;
                }

                return true;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }
        public async Task<IEnumerable<StorageRecord>> GetAllAsync()
        {
            await this.SetupStorageAsync();

            try
            {
                var query = this.docDb.CreateQuery<DocumentDbRecord>(this.client, this.storageConfig).ToList();

                IEnumerable<StorageRecord> storageRecords = query.Select(StorageRecord.FromDocumentDbRecord).ToList();

                // Delete expired records
                foreach (var record in storageRecords)
                {
                    if (record.IsExpired())
                    {
                        this.log.Debug("Deleting expired resource", () => new { this.storageName, record.Id, record.ETag });
                        await this.TryToDeleteExpiredRecord(record.Id);
                    }
                }

                return storageRecords.Where(x => !x.IsExpired());
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while retrieving the list of resources", () => new { this.storageName, e });
                throw;
            }
        }

        public async Task<StorageRecord> CreateAsync(StorageRecord input)
        {
            await this.SetupStorageAsync();

            try
            {
                var record = input.GetDocumentDbRecord();
                record.Touch();
                var response = await this.docDb.CreateAsync(this.client, this.storageConfig, record);
                return StorageRecord.FromDocumentDb(response?.Resource);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.Conflict)
            {
                this.log.Info("There is already a resource with the id specified.", () => new { this.storageName, input.Id });
                throw new ConflictingResourceException($"There is already a resource with id = '{input.Id}'.");
            }
        }

        public async Task<StorageRecord> UpsertAsync(StorageRecord input)
        {
            return await this.UpsertAsync(input, input.ETag);
        }

        public async Task<StorageRecord> UpsertAsync(StorageRecord input, string eTag)
        {
            await this.SetupStorageAsync();

            try
            {
                var record = input.GetDocumentDbRecord();
                record.Touch();
                var response = await this.docDb.UpsertAsync(this.client, this.storageConfig, record, eTag);
                return StorageRecord.FromDocumentDb(response?.Resource);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                this.log.Info(
                    "E-Tag mismatch: the resource has been updated by another client.",
                    () => new { this.storageName, input.Id, input.ETag });
                throw new ConflictingResourceException("E-Tag mismatch: the resource has been updated by another client.");
            }
        }

        public async Task DeleteAsync(string id)
        {
            await this.SetupStorageAsync();

            try
            {
                this.log.Debug("Deleting resource", () => new {this.storageName, id});
                await this.docDb.DeleteAsync(this.client, this.storageConfig, id);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                this.log.Debug("The resource requested doesn't exist, nothing to do.", () => new {this.storageName, id});
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

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
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
                var record = (await this.GetAsync(id)).GetDocumentDbRecord();

                if (record.IsLockedByOthers(ownerId, ownerType))
                {
                    this.log.Debug("The resource is locked by another client",
                        () => new { this.storageName, id, ownerId, ownerType });
                    return false;
                }

                record.Touch();
                record.Lock(ownerId, ownerType, durationSeconds);
                await this.docDb.UpsertAsync(this.client, this.storageConfig, record);

                return true;
            }
            catch (ResourceNotFoundException)
            {
                throw;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                this.log.Debug("E-Tag mismatch: the resource has been updated by another client and cannot be locked.",
                    () => new { this.storageName, id, ownerId, ownerType });
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                this.log.Warn("The resource doesn't exist and cannot be locked",
                    () => new { this.storageName, id, ownerId, ownerType });
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
                var record = (await this.GetAsync(id)).GetDocumentDbRecord();

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
                await this.docDb.UpsertAsync(this.client, this.storageConfig, record);
                return true;
            }
            catch (ResourceNotFoundException)
            {
                throw;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                this.log.Debug("E-Tag mismatch: the resource has been updated by another client and cannot be unlocked.",
                    () => new { this.storageName, id, ownerId, ownerType });
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                this.log.Warn("The resource doesn't exist and cannot be unlocked",
                    () => new { this.storageName, id, ownerId, ownerType });
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
                (this.client as IDisposable)?.Dispose();
            }

            this.disposedValue = true;
        }

        private async Task SetupStorageAsync()
        {
            this.instance.InitRequired();

            if (this.client == null)
            {
                this.log.Debug("Getting DocDb Client...", () => new { this.storageConfig.DocumentDbDatabase, this.storageConfig.DocumentDbCollection });
                this.client = await this.docDb.GetClientAsync(this.storageConfig);
                this.log.Debug("DocDb Client ready", () => new { this.storageConfig.DocumentDbDatabase, this.storageConfig.DocumentDbCollection });
            }
        }

        private async Task TryToDeleteExpiredRecord(string id)
        {
            try
            {
                await this.docDb.DeleteAsync(this.client, this.storageConfig, id);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                this.log.Debug("The resource requested doesn't exist, nothing to do.", () => new { this.storageName, id });
            }
            catch (Exception e)
            {
                this.log.Warn("Unable to delete the record due to an unexpected error.", () => new { this.storageName, id, e });
            }
        }
    }
}
