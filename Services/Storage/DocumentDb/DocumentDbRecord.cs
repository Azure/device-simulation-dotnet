// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.Documents;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.DocumentDb
{
    public class DocumentDbRecord : Resource
    {
        public const long NEVER = -1;

        public string Data { get; set; }

        public long ExpirationUtcMsecs { get; set; }
        public long LastModifiedUtcMsecs { get; set; }
        public string LockOwnerId { get; set; }
        public string LockOwnerType { get; set; }
        public long LockExpirationUtcMsecs { get; set; }

        public DocumentDbRecord()
        {
            this.Data = string.Empty;
            this.ExpirationUtcMsecs = NEVER;
            this.LastModifiedUtcMsecs = NEVER;

            this.LockOwnerId = string.Empty;
            this.LockOwnerType = string.Empty;
            this.LockExpirationUtcMsecs = NEVER;
        }

        public void Touch()
        {
            this.LastModifiedUtcMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public void Lock(string ownerId, string ownerType, long durationSecs)
        {
            this.LockOwnerId = ownerId;
            this.LockOwnerType = ownerType;
            this.LockExpirationUtcMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + durationSecs * 1000;
        }

        public bool IsLocked()
        {
            return this.LockExpirationUtcMsecs > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public bool IsLockedBy(string id, object o)
        {
            return this.LockOwnerId == id
                   && this.LockOwnerType == o.GetType().FullName;
        }

        public void ExpiresInMsecs(long durationMsecs)
        {
            this.ExpirationUtcMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + durationMsecs;
        }

        public bool IsExpired()
        {
            return this.ExpirationUtcMsecs != NEVER
                   && this.ExpirationUtcMsecs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
