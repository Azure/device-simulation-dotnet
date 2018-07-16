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
//     public interface IPartitionRecords
//     {
//         Task<StorageRecord> GetAsync(string id);
//         Task<IEnumerable<StorageRecord>> GetAllAsync();
//         Task<StorageRecord> CreateAsync(string id, StorageRecord input);
//         Task<StorageRecord> UpsertAsync(string id, StorageRecord input);
//         Task DeleteAsync(string id);
//     }
//
//     public class PartitionRecords : IPartitionRecords, IDisposable
//     {
//         private readonly ILogger log;
//         private readonly StorageConfig config;
//         private readonly IDocumentDbWrapper docDb;
//
//         private IDocumentClient client;
//         private bool disposedValue;
//
//         public PartitionRecords(
//             IServicesConfig config,
//             IDocumentDbWrapper docDb,
//             ILogger logger)
//         {
//             this.log = logger;
//             this.config = config.PartitionsStorage;
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
//                 return new StorageRecord(response);
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
//             var query = this.docDb.CreateQuery<DocumentDbRecord>(this.client, this.config).ToList();
//             return await Task.FromResult(query.Select(doc => new StorageRecord(doc)));
//         }
//
//         public async Task<StorageRecord> CreateAsync(string id, StorageRecord input)
//         {
//             await this.SetupStorageAsync();
//
//             try
//             {
//                 var response = await this.docDb.CreateAsync(
//                     this.client,
//                     this.config,
//                     new DocumentDbRecord(id, input.Data));
//                 return new StorageRecord(response);
//             }
//             catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.Conflict)
//             {
//                 this.log.Info("There is already a value with the id specified.", () => new { id });
//                 throw new ConflictingResourceException($"There is already a value with id = '{id}'.");
//             }
//         }
//
//         public async Task<StorageRecord> UpsertAsync(string id, StorageRecord input)
//         {
//             await this.SetupStorageAsync();
//
//             try
//             {
//                 var response = await this.docDb.UpsertAsync(
//                     this.client,
//                     this.config,
//                     new DocumentDbRecord(id, input.Data),
//                     input.ETag);
//                 return new StorageRecord(response);
//             }
//             catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed)
//             {
//                 this.log.Info("E-Tag mismatch: the resource has been updated by another client.", () => new { id, input.ETag });
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
