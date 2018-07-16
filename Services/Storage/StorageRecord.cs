// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Documents;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.DocumentDb;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage
{
    public class StorageRecord
    {
        private readonly DocumentDbRecord state;

        // Note: this.state.ETag is readonly
        public string ETag { get; private set; }

        public string Id
        {
            get => this.state.Id;
            set => this.state.Id = value;
        }

        // JSON serialized data
        public string Data
        {
            get => this.state.Data;
            set => this.state.Data = value;
        }

        public StorageRecord()
        {
            this.state = new DocumentDbRecord();
            this.ETag = string.Empty;
        }

        public DocumentDbRecord GetDocumentDbRecord()
        {
            return this.state;
        }

        public static StorageRecord FromDocumentDb(Document document)
        {
            if (document == null) return null;

            var result = new StorageRecord
            {
                Id = document.Id,
                ETag = document.ETag,
                Data = document.GetPropertyValue<string>("Data")
            };

            result.state.ExpirationUtcMsecs = document.GetPropertyValue<long>("ExpirationUtcMsecs");
            result.state.LastModifiedUtcMsecs = document.GetPropertyValue<long>("LastModifiedUtcMsecs");
            result.state.LockOwnerId = document.GetPropertyValue<string>("LockOwnerId");
            result.state.LockOwnerType = document.GetPropertyValue<string>("LockOwnerType");
            result.state.LockExpirationUtcMsecs = document.GetPropertyValue<long>("LockExpirationUtcMsecs");

            return result;
        }

        public static StorageRecord FromDocumentDbRecord(DocumentDbRecord document)
        {
            if (document == null) return null;

            var result = new StorageRecord
            {
                Id = document.Id,
                ETag = document.ETag,
                Data = document.Data
            };

            result.state.ExpirationUtcMsecs = document.ExpirationUtcMsecs;
            result.state.LastModifiedUtcMsecs = document.LastModifiedUtcMsecs;
            result.state.LockOwnerId = document.LockOwnerId;
            result.state.LockOwnerType = document.LockOwnerType;
            result.state.LockExpirationUtcMsecs = document.LockExpirationUtcMsecs;

            return result;
        }

        public void ExpiresInSecs(long secs)
        {
            this.state.ExpiresInMsecs(secs * 1000);
        }

        public void ExpiresInMsecs(long msecs)
        {
            this.state.ExpiresInMsecs(msecs);
        }

        public bool IsExpired()
        {
            return this.state.IsExpired();
        }

        // public bool IsLocked()
        // {
        //     return this.state.IsLocked();
        // }
        //
        // public bool IsLockedBy(string id, object o)
        // {
        //     return this.state.IsLockedBy(id, o);
        // }
    }
}
