// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Data;
using Microsoft.Azure.Documents;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.CosmosDbSql
{
    public class DataRecord : Resource, IDataRecord
    {
        public const long NEVER = -1;

        public string Data { get; set; }
        public long ExpirationUtcMsecs { get; set; }
        public long LastModifiedUtcMsecs { get; set; }
        public string LockOwnerId { get; set; }
        public string LockOwnerType { get; set; }
        public long LockExpirationUtcMsecs { get; set; }

        // "_etag" is the internal property used by the SDK
        private const string SDK_ETAG_FIELD = "_etag";

        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public DataRecord()
        {
            this.Data = string.Empty;
            this.ExpirationUtcMsecs = NEVER;
            this.LastModifiedUtcMsecs = NEVER;

            this.LockOwnerId = string.Empty;
            this.LockOwnerType = string.Empty;
            this.LockExpirationUtcMsecs = NEVER;
        }

        public string GetId()
        {
            return this.Id;
        }

        public string GetETag()
        {
            return this.ETag;
        }

        public IDataRecord SetETag(string eTag)
        {
            this.SetPropertyValue(SDK_ETAG_FIELD, eTag);
            return this;
        }

        public IDataRecord SetData(string data)
        {
            this.Data = data;
            return this;
        }

        public string GetData()
        {
            return this.Data;
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
    }
}
