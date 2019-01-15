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

        // Azure table columns can contain up to 64KB, and strings are encoded in UTF-16
        // These 12 properties allow to store 768KB of text, i.e. 384K chars
        // See https://docs.microsoft.com/rest/api/storageservices/Understanding-the-Table-Service-Data-Model
        public string Data01 { get; set; }
        public string Data02 { get; set; }
        public string Data03 { get; set; }
        public string Data04 { get; set; }
        public string Data05 { get; set; }
        public string Data06 { get; set; }
        public string Data07 { get; set; }
        public string Data08 { get; set; }
        public string Data09 { get; set; }
        public string Data10 { get; set; }
        public string Data11 { get; set; }
        public string Data12 { get; set; }

        public long ExpirationUtcMsecs { get; set; }
        public long LastModifiedUtcMsecs { get; set; }
        public string LockOwnerId { get; set; }
        public string LockOwnerType { get; set; }
        public long LockExpirationUtcMsecs { get; set; }

        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // C# strings are encoded in UTF16, so a string property with 32767 chars
        // requires 64KB, which is the limit for Azure Table columns
        private const int MAX_PROPERTY_LENGTH = 32767;
        private const int MAX_DATA_LENGTH = MAX_PROPERTY_LENGTH * 12;

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

        public IDataRecord SetETag(string eTag)
        {
            this.ETag = eTag;
            return this;
        }

        public IDataRecord SetData(string data)
        {
            this.Data01 = string.Empty;
            this.Data02 = string.Empty;
            this.Data03 = string.Empty;
            this.Data04 = string.Empty;
            this.Data05 = string.Empty;
            this.Data06 = string.Empty;
            this.Data07 = string.Empty;
            this.Data08 = string.Empty;
            this.Data09 = string.Empty;
            this.Data10 = string.Empty;
            this.Data11 = string.Empty;
            this.Data12 = string.Empty;

            var len = data.Length;

            if (len == 0) return this;

            if (len > MAX_DATA_LENGTH)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(data),
                    $"The content length ({len} chars) exceeds the " +
                    $"maximum record size ({MAX_DATA_LENGTH} chars)");
            }

            int count = (int) Math.Ceiling((double) len / MAX_PROPERTY_LENGTH);
            var parts = new string[count + 1];
            parts[count] = data.Substring(MAX_PROPERTY_LENGTH * (count - 1), len - MAX_PROPERTY_LENGTH * (count - 1));
            for (int i = 1; i < count; i++)
            {
                parts[i] = data.Substring(MAX_PROPERTY_LENGTH * (i - 1), MAX_PROPERTY_LENGTH);
            }

            if (count >= 1) this.Data01 = parts[1];
            if (count >= 2) this.Data02 = parts[2];
            if (count >= 3) this.Data03 = parts[3];
            if (count >= 4) this.Data04 = parts[4];
            if (count >= 5) this.Data05 = parts[5];
            if (count >= 6) this.Data06 = parts[6];
            if (count >= 7) this.Data07 = parts[7];
            if (count >= 8) this.Data08 = parts[8];
            if (count >= 9) this.Data09 = parts[9];
            if (count >= 10) this.Data10 = parts[10];
            if (count >= 11) this.Data11 = parts[11];
            if (count >= 12) this.Data12 = parts[12];

            return this;
        }

        public string GetData()
        {
            return string.Concat(
                this.Data01,
                this.Data02,
                this.Data03,
                this.Data04,
                this.Data05,
                this.Data06,
                this.Data07,
                this.Data08,
                this.Data09,
                this.Data10,
                this.Data11,
                this.Data12);
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
            this.SetData(string.Empty);

            this.ExpirationUtcMsecs = NEVER;
            this.LastModifiedUtcMsecs = NEVER;

            this.LockOwnerId = string.Empty;
            this.LockOwnerType = string.Empty;
            this.LockExpirationUtcMsecs = NEVER;
        }
    }
}
