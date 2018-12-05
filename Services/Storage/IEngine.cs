// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage
{
    public interface IEngine
    {
        IEngine Init(Config config);
        IDataRecord BuildRecord(string id);
        IDataRecord BuildRecord(string id, string data);
        Task<IDataRecord> GetAsync(string id);
        Task<bool> ExistsAsync(string id);
        Task<IEnumerable<IDataRecord>> GetAllAsync();
        Task<IDataRecord> CreateAsync(IDataRecord input);
        Task<IDataRecord> UpsertAsync(IDataRecord input);
        Task<IDataRecord> UpsertAsync(IDataRecord input, string eTag);
        Task DeleteAsync(string id);
        Task DeleteMultiAsync(List<string> ids);
        Task<bool> TryToLockAsync(string id, string ownerId, string ownerType, int durationSeconds);
        Task<bool> TryToUnlockAsync(string id, string ownerId, string ownerType);

        // TODO: implement this API to for scenarios where ETag mismatch doesn't matter
        //       and the client wants to do a delete+insert (overwrite)
        //Task<IDataRecord> DiscardAndUpsertAsync(IDataRecord input);
    }
}