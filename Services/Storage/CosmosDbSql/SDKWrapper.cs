// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.CosmosDbSql
{
    public interface ISDKWrapper
    {
        Task<IDocumentClient> GetClientAsync(Config config);

        Task<IResourceResponse<Document>> CreateAsync(
            IDocumentClient client,
            Config cfg,
            Resource resource);

        Task<IResourceResponse<Document>> UpsertAsync(
            IDocumentClient client,
            Config cfg,
            Resource resource);

        Task<IResourceResponse<Document>> UpsertAsync(
            IDocumentClient client,
            Config cfg,
            Resource resource,
            string eTag);

        Task<IResourceResponse<Document>> ReadAsync(
            IDocumentClient client,
            Config cfg,
            string docId);

        Task DeleteAsync(
            IDocumentClient client,
            Config cfg,
            string docId);

        IList<T> CreateQuery<T>(
            IDocumentClient client,
            Config cfg);
    }

    public class SDKWrapper : ISDKWrapper
    {
        private readonly ILogger log;

        public SDKWrapper(ILogger logger)
        {
            this.log = logger;
        }

        public async Task<IResourceResponse<Document>> CreateAsync(
            IDocumentClient client,
            Config cfg,
            Resource resource)
        {
            var collectionLink = $"/dbs/{cfg.CosmosDbSqlDatabase}/colls/{cfg.CosmosDbSqlCollection}";
            return await client.CreateDocumentAsync(collectionLink, resource);
        }

        public async Task<IResourceResponse<Document>> UpsertAsync(
            IDocumentClient client,
            Config cfg,
            Resource resource)
        {
            return await this.UpsertAsync(client, cfg, resource, resource.ETag);
        }

        public async Task<IResourceResponse<Document>> UpsertAsync(
            IDocumentClient client,
            Config cfg,
            Resource resource,
            string eTag)
        {
            var collectionLink = $"/dbs/{cfg.CosmosDbSqlDatabase}/colls/{cfg.CosmosDbSqlCollection}";

            return await client.UpsertDocumentAsync(
                collectionLink,
                resource,
                IfMatch(eTag));
        }

        public async Task<IResourceResponse<Document>> ReadAsync(
            IDocumentClient client,
            Config cfg,
            string docId)
        {
            var collectionLink = $"/dbs/{cfg.CosmosDbSqlDatabase}/colls/{cfg.CosmosDbSqlCollection}";
            return await client.ReadDocumentAsync($"{collectionLink}/docs/{docId}");
        }

        public async Task DeleteAsync(IDocumentClient client, Config cfg, string docId)
        {
            var collectionLink = $"/dbs/{cfg.CosmosDbSqlDatabase}/colls/{cfg.CosmosDbSqlCollection}";
            await client.DeleteDocumentAsync($"{collectionLink}/docs/{docId}");
        }

        public IList<T> CreateQuery<T>(IDocumentClient client, Config cfg)
        {
            var collectionLink = $"/dbs/{cfg.CosmosDbSqlDatabase}/colls/{cfg.CosmosDbSqlCollection}";
            return client.CreateDocumentQuery<T>(collectionLink).ToList();
        }

        public async Task<IDocumentClient> GetClientAsync(Config cfg)
        {
            const string FORMAT = "^AccountEndpoint=(?<endpoint>.*);AccountKey=(?<key>.*);$";

            var connstring = cfg.CosmosDbSqlConnString;

            var match = Regex.Match(connstring, FORMAT);
            if (!match.Success)
            {
                this.log.Error("Missing or invalid Cosmos DB SQL connection string ()", () => new { FORMAT, connstring });
                throw new InvalidConfigurationException($"Missing or invalid Cosmos DB SQL connection string ({FORMAT})");
            }

            var cosmosDbEndpoint = new Uri(match.Groups["endpoint"].Value);
            var cosmosDbKey = match.Groups["key"].Value;

            var cosmosDbOptions = new RequestOptions
            {
                OfferThroughput = cfg.CosmosDbSqlThroughput,
                ConsistencyLevel = ConsistencyLevel.Strong
            };

            var client = new DocumentClient(cosmosDbEndpoint, cosmosDbKey);

            await this.CreateDatabaseIfNotExistsAsync(client, cosmosDbOptions, cfg.CosmosDbSqlDatabase);
            await this.CreateCollectionIfNotExistsAsync(client, cosmosDbOptions, cfg.CosmosDbSqlDatabase, cfg.CosmosDbSqlCollection);

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
                this.log.Info("Checking if the database exists", () => new { uri });
                await client.ReadDatabaseAsync(uri, options);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                await this.CreateDatabaseAsync(client, db);
            }
            catch (Exception e)
            {
                this.log.Error("Error while getting Cosmos DB SQL database", e);
                throw;
            }
        }

        private async Task CreateDatabaseAsync(DocumentClient client, string dbName)
        {
            try
            {
                this.log.Info("Creating Cosmos DB SQL database",
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
                this.log.Error("Error while creating Cosmos DB SQL database", () => new { dbName, e });
                throw;
            }
        }

        private async Task CreateCollectionIfNotExistsAsync(
            IDocumentClient client,
            RequestOptions options,
            string dbName,
            string collName)
        {
            try
            {
                var uri = $"/dbs/{dbName}/colls/{collName}";
                this.log.Info("Checking if the collection exists", () => new { dbName, collName });
                await client.ReadDocumentCollectionAsync(uri, options);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                await this.CreateCollectionAsync(client, dbName, collName, options);
            }
            catch (Exception e)
            {
                this.log.Error("Error while getting Cosmos DB SQL collection", e);
                throw;
            }
        }

        private async Task CreateCollectionAsync(
            IDocumentClient client,
            string dbName,
            string collName,
            RequestOptions options)
        {
            try
            {
                this.log.Info("Creating Cosmos DB SQL collection",
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

                this.log.Error("Error while creating Cosmos DB SQL collection",
                    () => new { dbName, collName, e });

                throw new ExternalDependencyException("Error while creating Cosmos DB SQL collection", e);
            }
            catch (Exception e)
            {
                this.log.Error("Error while creating Cosmos DB SQL collection",
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
