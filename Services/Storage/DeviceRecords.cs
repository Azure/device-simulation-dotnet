// // Copyright (c) Microsoft. All rights reserved.
//
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Net;
// using System.Threading.Tasks;
// using Microsoft.Azure.Documents;
// using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
// using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
// using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
// using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.DocumentDb;
//
// namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage
// {
//     public interface IDeviceRecords
//     {
//         Task<StorageRecord> GetAsync(string id);
//         Task<IEnumerable<StorageRecord>> GetAllAsync();
//         Task<StorageRecord> CreateAsync(string id, StorageRecord input);
//         Task<StorageRecord> UpsertAsync(string id, StorageRecord input);
//         Task DeleteAsync(string id);
//     }
//
//     public class DeviceRecords : IDeviceRecords, IDisposable
//     {
//         private readonly ILogger log;
//         private readonly StorageConfig config;
//         private readonly IDocumentDbWrapper docDb;
//
//         private IDocumentClient client;
//         private bool disposedValue;
//
//         public DeviceRecords(
//             IServicesConfig config,
//             IDocumentDbWrapper docDb,
//             ILogger logger)
//         {
//             this.log = logger;
//             this.config = config.DevicesStorage;
//             this.docDb = docDb;
//             this.disposedValue = false;
//             this.client = null;
//         }
//
//         public async Task<StorageRecord> GetAsync(string id)
//         {
//             await this.SetupStorageAsync();
//
//             try
//             {
//                 var response = await this.docDb.ReadAsync(this.client, this.config, id);
//                 var record = StorageRecord.FromDocumentDb(response?.Resource);
//                 return record;
//             }
//             catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
//             {
//                 this.log.Info("The resource requested doesn't exist.", () => new { id });
//                 throw new ResourceNotFoundException($"The resource {id} doesn't exist.");
//             }
//         }
//
//         public async Task DeleteAsync(string id)
//         {
//             await this.SetupStorageAsync();
//
//             try
//             {
//                 await this.docDb.DeleteAsync(this.client, this.config, id);
//             }
//             catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
//             {
//                 this.log.Debug("The resource requested doesn't exist, nothing to do.", () => new { id });
//             }
//         }
//
//         public async Task<IEnumerable<StorageRecord>> GetAllAsync()
//         {
//             await this.SetupStorageAsync();
//
//             try
//             {
//                 var query = this.docDb.CreateQuery<DocumentDbRecord>(this.client, this.config).ToList();
//                 IEnumerable<StorageRecord> storageRecords = query.Select(doc => StorageRecord.FromDocumentDbRecord(doc)).ToList();
//                 return storageRecords;
//             }
//             catch (Exception e)
//             {
//                 this.log.Error("Unexpected error while retrieving the list of resources", () => new { e });
//                 throw;
//             }
//         }
//
//         public async Task<StorageRecord> CreateAsync(string id, StorageRecord input)
//         {
//             await this.SetupStorageAsync();
//
//             try
//             {
//                 var record = new DocumentDbRecord
//                 {
//                     Id = id,
//                     Data = input.Data
//                 };
//                 record.Touch();
//
//                 var response = await this.docDb.CreateAsync(
//                     this.client,
//                     this.config,
//                     record);
//
//                 return StorageRecord.FromDocumentDb(response?.Resource);
//             }
//             catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.Conflict)
//             {
//                 this.log.Info("There is already a resource with the id specified.", () => new { id });
//                 throw new ConflictingResourceException($"There is already a resource with id = '{id}'.");
//             }
//         }
//
//         public async Task<StorageRecord> UpsertAsync(string id, StorageRecord input)
//         {
//             await this.SetupStorageAsync();
//
//             try
//             {
//                 var record = input.GetDocumentDbRecord();
//                 record.Touch();
//                 var response = await this.docDb.UpsertAsync(this.client, this.config, record);
//                 return StorageRecord.FromDocumentDb(response?.Resource);
//             }
//             catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed)
//             {
//                 this.log.Info(
//                     "E-Tag mismatch: the resource has been updated by another client.",
//                     () => new { id, input.ETag });
//                 throw new ConflictingResourceException("E-Tag mismatch: the resource has been updated by another client.");
//             }
//         }
//
//         public void Dispose()
//         {
//             this.Dispose(true);
//         }
//
//         private async Task SetupStorageAsync()
//         {
//             if (this.client == null)
//             {
//                 this.client = await this.docDb.GetClientAsync(this.config);
//             }
//         }
//
//         private void Dispose(bool disposing)
//         {
//             if (!this.disposedValue && disposing)
//             {
//                 (this.client as IDisposable)?.Dispose();
//             }
//
//             this.disposedValue = true;
//         }
//     }
// }
