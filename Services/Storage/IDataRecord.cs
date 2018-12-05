// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage
{
    public interface IDataRecord
    {
        // Record ID. The underlying implementation depends on the storage engine,
        // for instance table storage uses PK and RK
        string GetId();

        // ETag string, the value is managed directly by the storage engine in use
        string GetETag();

        // ETag string. The value is not a "Property" to avoid it being saved twice.
        IDataRecord SetETag(string eTag);

        // JSON serialized data from the business layer
        IDataRecord SetData(string data);

        // JSON serialized data from the business layer
        string GetData();

        // Set the record expiration
        void ExpiresInSecs(long secs);

        // Return true if the record is expired
        bool IsExpired();

        // Return true if the record is locked by some client
        bool IsLocked();

        // Return true if the record is locke by another client
        bool IsLockedByOthers(string ownerId, string ownerType);

        // Lock the record
        void Lock(string ownerId, string ownerType, long durationSeconds);

        // Return true if the client can unlock the record
        bool CanUnlock(string ownerId, string ownerType);

        // Unlock the record
        void Unlock(string ownerId, string ownerType);

        // Change the record last modified time
        void Touch();
    }
}
