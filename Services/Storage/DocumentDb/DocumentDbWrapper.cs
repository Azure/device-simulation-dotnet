// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.DocumentDb
{
    public interface IDocumentDbWrapper
    {
        Task<IDocumentClient> GetClientAsync(StorageConfig config);

        Task<IResourceResponse<Document>> CreateAsync(
            IDocumentClient client,
            StorageConfig cfg,
            Resource resource);

        Task<IResourceResponse<Document>> UpsertAsync(
            IDocumentClient client,
            StorageConfig cfg,
            Resource resource);

        Task<IResourceResponse<Document>> UpsertAsync(
            IDocumentClient client,
            StorageConfig cfg,
            Resource resource,
            string eTag);

        Task<IResourceResponse<Document>> ReadAsync(
            IDocumentClient client,
            StorageConfig cfg,
            string docId);

        Task DeleteAsync(
            IDocumentClient client,
            StorageConfig cfg,
            string docId);

        IOrderedQueryable<T> CreateQuery<T>(
            IDocumentClient client,
            StorageConfig cfg);

        IQueryable<T> CreateQuery<T>(
            IDocumentClient client,
            StorageConfig cfg,
            string sqlCondition,
            SqlParameter[] parameters);
    }

    public class DocumentDbWrapper : IDocumentDbWrapper
    {
        private readonly ILogger log;

        public DocumentDbWrapper(ILogger logger)
        {
            this.log = logger;
        }

        public async Task<IResourceResponse<Document>> CreateAsync(
            IDocumentClient client,
            StorageConfig cfg,
            Resource resource)
        {
            var collectionLink = $"/dbs/{cfg.DocumentDbDatabase}/colls/{cfg.DocumentDbCollection}";
            return await client.CreateDocumentAsync(collectionLink, resource);
        }

        public async Task<IResourceResponse<Document>> UpsertAsync(
            IDocumentClient client,
            StorageConfig cfg,
            Resource resource)
        {
            return await this.UpsertAsync(client, cfg, resource, resource.ETag);
        }

        public async Task<IResourceResponse<Document>> UpsertAsync(
            IDocumentClient client,
            StorageConfig cfg,
            Resource resource,
            string eTag)
        {
            var collectionLink = $"/dbs/{cfg.DocumentDbDatabase}/colls/{cfg.DocumentDbCollection}";

            return await client.UpsertDocumentAsync(
                collectionLink,
                resource,
                IfMatch(eTag));
        }

        public async Task<IResourceResponse<Document>> ReadAsync(
            IDocumentClient client,
            StorageConfig cfg,
            string docId)
        {
            var collectionLink = $"/dbs/{cfg.DocumentDbDatabase}/colls/{cfg.DocumentDbCollection}";
            return await client.ReadDocumentAsync($"{collectionLink}/docs/{docId}");
        }

        public async Task DeleteAsync(IDocumentClient client, StorageConfig cfg, string docId)
        {
            var collectionLink = $"/dbs/{cfg.DocumentDbDatabase}/colls/{cfg.DocumentDbCollection}";
            await client.DeleteDocumentAsync($"{collectionLink}/docs/{docId}");
        }

        public IOrderedQueryable<T> CreateQuery<T>(IDocumentClient client, StorageConfig cfg)
        {
            var collectionLink = $"/dbs/{cfg.DocumentDbDatabase}/colls/{cfg.DocumentDbCollection}";
            return client.CreateDocumentQuery<T>(collectionLink);
        }

        public IQueryable<T> CreateQuery<T>(
            IDocumentClient client,
            StorageConfig cfg,
            string sqlCondition,
            SqlParameter[] parameters)
        {
            var collectionLink = $"/dbs/{cfg.DocumentDbDatabase}/colls/{cfg.DocumentDbCollection}";
            var query = new SqlQuerySpec(
                "SELECT * FROM ROOT WHERE " + sqlCondition,
                new SqlParameterCollection(parameters));
            return client.CreateDocumentQuery<T>(collectionLink, query);
        }

        public async Task<IDocumentClient> GetClientAsync(StorageConfig cfg)
        {
            const string FORMAT = "^AccountEndpoint=(?<endpoint>.*);AccountKey=(?<key>.*);$";

            var connstring = cfg.DocumentDbConnString;

            var match = Regex.Match(connstring, FORMAT);
            if (!match.Success)
            {
                this.log.Error("Missing or invalid DocumentDb connection string ()", () => new { FORMAT, connstring });
                throw new InvalidConfigurationException($"Missing or invalid DocumentDb connection string ({FORMAT})");
            }

            var docDbEndpoint = new Uri(match.Groups["endpoint"].Value);
            var docDbKey = match.Groups["key"].Value;

            var docDbOptions = new RequestOptions
            {
                OfferThroughput = cfg.DocumentDbThroughput,
                ConsistencyLevel = ConsistencyLevel.Strong
            };

            var client = new DocumentClient(docDbEndpoint, docDbKey);

            await this.CreateDatabaseIfNotExistsAsync(client, docDbOptions, cfg.DocumentDbDatabase);
            await this.EnsureCollectionExistsAsync(client, docDbOptions, cfg.DocumentDbDatabase, cfg.DocumentDbCollection);

            return client;
        }

        private async Task CreateDatabaseIfNotExistsAsync(
            DocumentClient client,
            RequestOptions options,
            string db)
        {
            try
            {
                var uri = "/dbs/" + db;
                await client.ReadDatabaseAsync(uri, options);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                await this.CreateDatabaseAsync(client, db);
            }
            catch (Exception e)
            {
                this.log.Error("Error while getting DocumentDb database", e);
                throw;
            }
        }

        private async Task CreateDatabaseAsync(DocumentClient client, string dbName)
        {
            try
            {
                this.log.Info("Creating DocumentDb database",
                    () => new { dbName });
                var db = new Database { Id = dbName };
                await client.CreateDatabaseAsync(db);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.Conflict)
            {
                this.log.Warn("Another process already created the database", () => new { dbName });
            }
            catch (Exception e)
            {
                this.log.Error("Error while creating DocumentDb database", () => new { dbName, e });
                throw;
            }
        }

        private async Task EnsureCollectionExistsAsync(
            IDocumentClient client,
            RequestOptions options,
            string dbName,
            string collName)
        {
            try
            {
                var uri = $"/dbs/{dbName}/colls/{collName}";
                await client.ReadDocumentCollectionAsync(uri, options);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                await this.CreateCollectionIfNotExistsAsync(client, dbName, collName, options);
            }
            catch (Exception e)
            {
                this.log.Error("Error while getting DocumentDb collection", e);
                throw;
            }
        }

        private async Task CreateCollectionIfNotExistsAsync(
            IDocumentClient client,
            string dbName,
            string collName,
            RequestOptions options)
        {
            try
            {
                this.log.Info("Creating DocumentDb collection",
                    () => new { dbName, collName });
                var coll = new DocumentCollection { Id = collName };

                var index = Index.Range(DataType.String, -1);
                var indexing = new IndexingPolicy(index) { IndexingMode = IndexingMode.Consistent };
                coll.IndexingPolicy = indexing;

                var dbUri = "/dbs/" + dbName;
                await client.CreateDocumentCollectionAsync(dbUri, coll, options);
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.Conflict)
                {
                    this.log.Warn("Another process already created the collection",
                        () => new { dbName, collName });
                    // Don't throw exception because it's fine if the collection was created somewhere else
                    return;
                }

                this.log.Error("Error while creating DocumentDb collection",
                    () => new { dbName, collName, e });

                throw new ExternalDependencyException("Error while creating DocumentDb collection", e);
            }
            catch (Exception e)
            {
                this.log.Error("Error while creating DocumentDb collection",
                    () => new { dbName, collName, e });
                throw;
            }
        }

        private static RequestOptions IfMatch(string etag)
        {
            if (etag == "*") return null;

            return new RequestOptions
            {
                AccessCondition = new AccessCondition
                {
                    Condition = etag,
                    Type = AccessConditionType.IfMatch
                }
            };
        }
    }
}
