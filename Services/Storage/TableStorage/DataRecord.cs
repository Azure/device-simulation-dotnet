// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.TableStorage
{
    public class DataRecord : TableEntity, IDataRecord
    {
        public const string FIXED_PKEY = "1";
        public const long NEVER = -1;

        public string Data { get; set; }
        public long ExpirationUtcMsecs { get; set; }
        public long LastModifiedUtcMsecs { get; set; }
        public string LockOwnerId { get; set; }
        public string LockOwnerType { get; set; }
        public long LockExpirationUtcMsecs { get; set; }

        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // A table entity type must expose a parameter-less constructor
        public DataRecord()
        {
            this.SetDefaults();
        }

        // Define PK and RK
        public DataRecord(string id)
        {
            this.SetDefaults();

            // Records are not partitioned for now
            this.PartitionKey = FIXED_PKEY;
            this.RowKey = id;
        }

        public string GetId()
        {
            return this.RowKey;
        }

        public string GetETag()
        {
            return this.ETag;
        }

        public void SetETag(string eTag)
        {
            this.ETag = eTag;
        }

        public void Touch()
        {
            this.LastModifiedUtcMsecs = Now;
        }

        public void Unlock(string ownerId, string ownerType)
        {
            // Nothing to do
            if (this.LockExpirationUtcMsecs < Now) return;

            ownerType = ownerType ?? string.Empty;

            if (this.LockOwnerId != ownerId || this.LockOwnerType != ownerType)
            {
                throw new ResourceIsLockedByAnotherOwnerException();
            }

            this.LockOwnerId = string.Empty;
            this.LockExpirationUtcMsecs = 0;
        }

        public bool CanUnlock(string ownerId, string ownerType)
        {
            ownerType = ownerType ?? string.Empty;

            return this.LockExpirationUtcMsecs < Now
                   || (this.LockOwnerId == ownerId && this.LockOwnerType == ownerType);
        }

        public bool IsLocked()
        {
            return this.LockExpirationUtcMsecs > Now;
        }

        public bool IsLockedBy(string ownerId, string ownerType)
        {
            ownerType = ownerType ?? string.Empty;

            return this.IsLocked()
                   && this.LockOwnerId == ownerId
                   && this.LockOwnerType == ownerType;
        }

        public bool IsLockedByOthers(string ownerId, string ownerType)
        {
            ownerType = ownerType ?? string.Empty;

            return this.IsLocked()
                   && (this.LockOwnerId != ownerId || this.LockOwnerType != ownerType);
        }

        public void ExpiresInMsecs(long durationMsecs)
        {
            this.ExpirationUtcMsecs = Now + durationMsecs;
        }

        public void ExpiresInSecs(long secs)
        {
            this.ExpiresInMsecs(secs * 1000);
        }

        public bool IsExpired()
        {
            return this.ExpirationUtcMsecs != NEVER
                   && this.ExpirationUtcMsecs <= Now;
        }

        public void Lock(string ownerId, string ownerType, long durationSeconds)
        {
            ownerType = ownerType ?? string.Empty;

            this.LockOwnerId = ownerId;
            this.LockOwnerType = ownerType;
            this.LockExpirationUtcMsecs = Now + durationSeconds * 1000;
        }

        private void SetDefaults()
        {
            this.Data = string.Empty;
            this.ExpirationUtcMsecs = NEVER;
            this.LastModifiedUtcMsecs = NEVER;

            this.LockOwnerId = string.Empty;
            this.LockOwnerType = string.Empty;
            this.LockExpirationUtcMsecs = NEVER;
        }
    }
}
