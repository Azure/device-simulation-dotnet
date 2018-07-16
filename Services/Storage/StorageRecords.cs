// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.DocumentDb;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage
{
    public interface IStorageRecords
    {
        IStorageRecords Setup(StorageConfig config);
        Task<StorageRecord> GetAsync(string id);
        Task<bool> ExistsAsync(string id);
        Task<IEnumerable<StorageRecord>> GetAllAsync();
        Task<StorageRecord> CreateAsync(StorageRecord input);
        Task<StorageRecord> UpsertAsync(StorageRecord input);
        Task<StorageRecord> UpsertAsync(StorageRecord input, string eTag);
        Task DeleteAsync(string id);
        Task DeleteMultiAsync(List<string> ids);
        Task<bool> TryToLockAsync(string id, string ownerId, object owner, int durationSecs);
    }

    public class StorageRecords : IStorageRecords, IDisposable
    {
        private readonly ILogger log;
        private StorageConfig config;
        private readonly IDocumentDbWrapper docDb;

        private IDocumentClient client;
        private bool disposedValue;
        private string storageName;

        public StorageRecords(
            IServicesConfig config,
            IDocumentDbWrapper docDb,
            ILogger logger)
        {
            this.log = logger;
            this.docDb = docDb;
            this.disposedValue = false;
            this.config = null;
            this.client = null;
        }

        public IStorageRecords Setup(StorageConfig cfg)
        {
            this.config = cfg;

            // Used only for logging
            this.storageName = cfg.DocumentDbDatabase + "/" + cfg.DocumentDbCollection;

            return this;
        }

        public async Task<StorageRecord> GetAsync(string id)
        {
            await this.SetupStorageAsync();

            try
            {
                this.log.Debug("Fetching record...", () => new { id });
                var response = await this.docDb.ReadAsync(this.client, this.config, id);
                this.log.Debug("Record fetched", () => new { id });
                
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
                this.log.Debug("The resource requested doesn't exist.", () => new { this.storageName, id });
                throw new ResourceNotFoundException($"The resource {id} doesn't exist.");
            }
        }

        public async Task<bool> ExistsAsync(string id)
        {
            await this.SetupStorageAsync();

            try
            {
                var response = await this.docDb.ReadAsync(this.client, this.config, id);
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

        public async Task DeleteAsync(string id)
        {
            await this.SetupStorageAsync();

            try
            {
                this.log.Debug("Deleting resource", () => new { this.storageName, id });
                await this.docDb.DeleteAsync(this.client, this.config, id);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                this.log.Debug("The resource requested doesn't exist, nothing to do.", () => new { this.storageName, id });
            }
        }

        public async Task DeleteMultiAsync(List<string> ids)
        {
            await this.SetupStorageAsync();

            const int MAX_PENDING_TASKS = 25;
            
            var tasks = new List<Task>();
            foreach (var id in ids)
            {
                tasks.Add(this.DeleteAsync(id));
                if (tasks.Count < MAX_PENDING_TASKS) continue;
                
                await Task.WhenAll(tasks);
                tasks.Clear();
            }
            
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        public async Task<IEnumerable<StorageRecord>> GetAllAsync()
        {
            await this.SetupStorageAsync();

            try
            {
                var query = this.docDb.CreateQuery<DocumentDbRecord>(this.client, this.config).ToList();

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

            /*
            var sqlCondition = "Expiration > @expiration";
            var sqlParams = new[]
            {
                new SqlParameter
                {
                    Name = "@expiration",
                    Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }
            };

            var query = this.docDb.CreateQuery<DocumentDbRecord>(
                this.client,
                this.config,
                sqlCondition,
                sqlParams).ToList();

            return await Task.FromResult(query.Select(doc => new StorageRecord(doc)));
            */
        }

        public async Task<StorageRecord> CreateAsync(StorageRecord input)
        {
            await this.SetupStorageAsync();

            try
            {
                var record = input.GetDocumentDbRecord();
                record.Touch();
                var response = await this.docDb.CreateAsync(this.client, this.config, record);
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
                var response = await this.docDb.UpsertAsync(this.client, this.config, record, eTag);
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

        public async Task<bool> TryToLockAsync(
            string id,
            string ownerId,
            object owner,
            int durationSecs)
        {
            await this.SetupStorageAsync();

            try
            {
                var record = (await this.GetAsync(id)).GetDocumentDbRecord();

                if (record.IsLocked() && !record.IsLockedBy(ownerId, owner))
                {
                    throw new ResourceIsLockedException();
                }

                record.Touch();
                record.Lock(ownerId, owner.GetType().FullName, durationSecs);
                await this.docDb.UpsertAsync(this.client, this.config, record);

                return true;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                this.log.Debug(
                    "E-Tag mismatch: the resource has been updated by another client and cannot be locked.",
                    () => new { this.storageName, id, ownerId, owner });
                return false;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                this.log.Warn(
                    "The resource doesn't exist and cannot be locked",
                    () => new { this.storageName, id, ownerId, owner });
                return false;
            }
            catch (ResourceIsLockedException)
            {
                this.log.Debug(
                    "The resource is locked by another client",
                    () => new { this.storageName, id, ownerId, owner });
                return false;
            }
            catch (Exception e)
            {
                this.log.Error(
                    "An unexpected error occurred while writing to the storage",
                    () => new { this.storageName, id, ownerId, owner, lockDurationSecs = durationSecs, e });
                return false;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        private async Task TryToDeleteExpiredRecord(string id)
        {
            try
            {
                await this.docDb.DeleteAsync(this.client, this.config, id);
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

        private async Task SetupStorageAsync()
        {
            if (this.config == null)
            {
                throw new ApplicationException("Class not initialized. Setup() must be called before calling any other method.");
            }

            if (this.client == null)
            {
                this.log.Debug("Getting DocDb Client...", () => { });
                this.client = await this.docDb.GetClientAsync(this.config);
                this.log.Debug("DocDb Client ready", () => { });
            }
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposedValue && disposing)
            {
                (this.client as IDisposable)?.Dispose();
            }

            this.disposedValue = true;
        }
    }
}
