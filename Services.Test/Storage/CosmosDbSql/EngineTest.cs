// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.CosmosDbSql;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.Storage.CosmosDbSql
{
    public class EngineTest
    {
        private readonly Engine target;

        private readonly Mock<IFactory> factory;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IInstance> instance;
        private readonly Mock<ISDKWrapper> cosmosDbSql;
        private readonly Mock<IDocumentClient> cosmosDbSqlClient;

        private readonly Config storageConfig;

        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private static readonly long testOffsetMs = 100000;

        public EngineTest()
        {
            this.factory = new Mock<IFactory>();
            this.logger = new Mock<ILogger>();
            this.instance = new Mock<IInstance>();

            this.storageConfig = new Config { CosmosDbSqlDatabase = "db", CosmosDbSqlCollection = "coll" };

            // Mock storage wrapper and client
            this.cosmosDbSql = new Mock<ISDKWrapper>();
            this.factory.Setup(x => x.Resolve<ISDKWrapper>()).Returns(this.cosmosDbSql.Object);
            this.cosmosDbSqlClient = new Mock<IDocumentClient>();
            this.cosmosDbSql.Setup(x => x.GetClientAsync(this.storageConfig)).ReturnsAsync(this.cosmosDbSqlClient.Object);

            this.target = new Engine(this.factory.Object, this.logger.Object, this.instance.Object);

            this.target.Init(this.storageConfig);
            this.instance.Invocations.Clear();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanBeInitializedOnlyOnce()
        {
            // Act
            var engine = new Engine(this.factory.Object, this.logger.Object, this.instance.Object);
            engine.Init(this.storageConfig);

            // Assert
            this.instance.Verify(x => x.InitComplete(), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItProvidesRecordBuilders()
        {
            // Act
            var x = this.target.BuildRecord("123");
            var y = this.target.BuildRecord("345", "someData");

            // Assert
            Assert.Equal("123", x.GetId());
            Assert.Empty(x.GetData());
            Assert.Null(x.GetETag());
            Assert.Equal("345", y.GetId());
            Assert.Equal("someData", y.GetData());
            Assert.Null(y.GetETag());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsWhenFetchingARecordThatDoesntExist()
        {
            // Arrange
            const string ID = "ghd";
            this.cosmosDbSql.Setup(x => x.ReadAsync(this.cosmosDbSqlClient.Object, this.storageConfig, ID))
                .Throws(BuildDocumentClientException(HttpStatusCode.NotFound));

            // Act - Assert
            Assert.ThrowsAsync<ResourceNotFoundException>(
                async () => await this.target.GetAsync(ID)).CompleteOrTimeout();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDeletesExpiredRecordsOnFetch()
        {
            // Arrange
            const string ID_NEW = "new";
            const string ID_OLD = "old";
            IResourceResponse<Document> response1 = BuildResponseWithDocument(id: ID_NEW, expired: false);
            IResourceResponse<Document> response2 = BuildResponseWithDocument(id: ID_OLD, expired: true);
            this.cosmosDbSql.Setup(x => x.ReadAsync(It.IsAny<IDocumentClient>(), this.storageConfig, ID_NEW))
                .ReturnsAsync(response1);
            this.cosmosDbSql.Setup(x => x.ReadAsync(It.IsAny<IDocumentClient>(), this.storageConfig, ID_OLD))
                .ReturnsAsync(response2);

            // Act
            this.target.GetAsync(ID_NEW).CompleteOrTimeout();
            Assert.ThrowsAsync<ResourceNotFoundException>(
                async () => await this.target.GetAsync(ID_OLD)).CompleteOrTimeout();

            // Assert
            this.cosmosDbSql.Verify(x => x.DeleteAsync(this.cosmosDbSqlClient.Object, this.storageConfig, ID_NEW), Times.Never);
            this.cosmosDbSql.Verify(x => x.DeleteAsync(this.cosmosDbSqlClient.Object, this.storageConfig, ID_OLD), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDeletesExpiredRecordsOnExistsCheck()
        {
            // Arrange
            const string ID_NEW = "new";
            const string ID_OLD = "old";
            IResourceResponse<Document> response1 = BuildResponseWithDocument(id: ID_NEW, expired: false);
            IResourceResponse<Document> response2 = BuildResponseWithDocument(id: ID_OLD, expired: true);
            this.cosmosDbSql.Setup(x => x.ReadAsync(It.IsAny<IDocumentClient>(), this.storageConfig, ID_NEW))
                .ReturnsAsync(response1);
            this.cosmosDbSql.Setup(x => x.ReadAsync(It.IsAny<IDocumentClient>(), this.storageConfig, ID_OLD))
                .ReturnsAsync(response2);

            // Act
            this.target.ExistsAsync(ID_NEW).CompleteOrTimeout();
            this.target.ExistsAsync(ID_OLD).CompleteOrTimeout();

            // Assert
            this.cosmosDbSql.Verify(x => x.DeleteAsync(this.cosmosDbSqlClient.Object, this.storageConfig, ID_NEW), Times.Never);
            this.cosmosDbSql.Verify(x => x.DeleteAsync(this.cosmosDbSqlClient.Object, this.storageConfig, ID_OLD), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsWhenAGenericExceptionHappens()
        {
            // Arrange
            this.cosmosDbSql.Setup(x => x.ReadAsync(this.cosmosDbSqlClient.Object, this.storageConfig, It.IsAny<string>()))
                .Throws<SomeException>();
            this.cosmosDbSql.Setup(x => x.CreateQuery<DataRecord>(this.cosmosDbSqlClient.Object, this.storageConfig))
                .Throws<SomeException>();
            this.cosmosDbSql.Setup(x => x.CreateAsync(this.cosmosDbSqlClient.Object, this.storageConfig, It.IsAny<Resource>()))
                .Throws<SomeException>();
            this.cosmosDbSql.Setup(x => x.UpsertAsync(this.cosmosDbSqlClient.Object, this.storageConfig, It.IsAny<Resource>()))
                .Throws<SomeException>();
            this.cosmosDbSql.Setup(x => x.UpsertAsync(this.cosmosDbSqlClient.Object, this.storageConfig, It.IsAny<Resource>(), It.IsAny<string>()))
                .Throws<SomeException>();
            this.cosmosDbSql.Setup(x => x.DeleteAsync(this.cosmosDbSqlClient.Object, this.storageConfig, It.IsAny<string>()))
                .Throws<SomeException>();

            // Act - Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                async () => await this.target.GetAsync("x")).CompleteOrTimeout();
            Assert.ThrowsAsync<ExternalDependencyException>(
                async () => await this.target.ExistsAsync("x")).CompleteOrTimeout();
            Assert.ThrowsAsync<ExternalDependencyException>(
                async () => await this.target.GetAllAsync()).CompleteOrTimeout();
            Assert.ThrowsAsync<ExternalDependencyException>(
                async () => await this.target.CreateAsync(new DataRecord())).CompleteOrTimeout();
            Assert.ThrowsAsync<ExternalDependencyException>(
                async () => await this.target.UpsertAsync(new DataRecord())).CompleteOrTimeout();
            Assert.ThrowsAsync<ExternalDependencyException>(
                async () => await this.target.UpsertAsync(new DataRecord(), "someETag")).CompleteOrTimeout();
            Assert.ThrowsAsync<ExternalDependencyException>(
                async () => await this.target.DeleteAsync("someId")).CompleteOrTimeout();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanCheckIfARecordExists()
        {
            // Arrange
            const string ID_NEW = "new";
            const string ID_OLD = "old";
            const string ID_MISSING = "xyz";
            IResourceResponse<Document> response1 = BuildResponseWithDocument(id: ID_NEW, expired: false);
            IResourceResponse<Document> response2 = BuildResponseWithDocument(id: ID_OLD, expired: true);
            this.cosmosDbSql.Setup(x => x.ReadAsync(It.IsAny<IDocumentClient>(), this.storageConfig, ID_NEW))
                .ReturnsAsync(response1);
            this.cosmosDbSql.Setup(x => x.ReadAsync(It.IsAny<IDocumentClient>(), this.storageConfig, ID_OLD))
                .ReturnsAsync(response2);

            // Act
            var result1 = this.target.ExistsAsync(ID_NEW).CompleteOrTimeout().Result;
            var result2 = this.target.ExistsAsync(ID_OLD).CompleteOrTimeout().Result;
            var result3 = this.target.ExistsAsync(ID_MISSING).CompleteOrTimeout().Result;

            // Assert
            Assert.True(result1);
            Assert.False(result2);
            Assert.False(result3);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItFetchesAllRecordsDeletingTheExpiredOnes()
        {
            // Arrange
            var data = new List<DataRecord>
            {
                new DataRecord { Id = "old1", ExpirationUtcMsecs = Now - testOffsetMs },
                new DataRecord { Id = "A", ExpirationUtcMsecs = Now + testOffsetMs },
                new DataRecord { Id = "old2", ExpirationUtcMsecs = Now - testOffsetMs },
                new DataRecord { Id = "B", ExpirationUtcMsecs = Now + testOffsetMs },
                new DataRecord { Id = "old3", ExpirationUtcMsecs = Now - testOffsetMs }
            };
            this.cosmosDbSql.Setup(x => x.CreateQuery<DataRecord>(this.cosmosDbSqlClient.Object, this.storageConfig))
                .Returns(data);

            // Act
            IEnumerable<IDataRecord> result = this.target.GetAllAsync().CompleteOrTimeout().Result;

            // Assert
            Assert.Equal(2, result.Count());
            this.cosmosDbSql.Verify(x => x.DeleteAsync(this.cosmosDbSqlClient.Object, this.storageConfig, It.IsAny<string>()), Times.Exactly(3));
            this.cosmosDbSql.Verify(x => x.DeleteAsync(this.cosmosDbSqlClient.Object, this.storageConfig, "old1"), Times.Once);
            this.cosmosDbSql.Verify(x => x.DeleteAsync(this.cosmosDbSqlClient.Object, this.storageConfig, "old2"), Times.Once);
            this.cosmosDbSql.Verify(x => x.DeleteAsync(this.cosmosDbSqlClient.Object, this.storageConfig, "old3"), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanCreateNewRecords()
        {
            // Arrange
            var record = new DataRecord();

            // Act
            this.target.CreateAsync(record).CompleteOrTimeout();

            // Assert
            this.cosmosDbSql.Verify(x => x.CreateAsync(this.cosmosDbSqlClient.Object, this.storageConfig, record), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsWhenCreatingAConflictingRecord()
        {
            // Arrange
            var record = new DataRecord();
            this.cosmosDbSql.Setup(x => x.CreateAsync(this.cosmosDbSqlClient.Object, this.storageConfig, record))
                .Throws(BuildDocumentClientException(HttpStatusCode.Conflict));

            // Act - Assert
            Assert.ThrowsAsync<ConflictingResourceException>(
                async () => await this.target.CreateAsync(record)).CompleteOrTimeout();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanModifyARecordGivenTheRightETag()
        {
            // Arrange
            var record = new DataRecord();
            record.SetETag("foo");

            // Act
            this.target.UpsertAsync(record).CompleteOrTimeout();

            // Assert
            this.cosmosDbSql.Verify(x => x.UpsertAsync(this.cosmosDbSqlClient.Object, this.storageConfig, It.IsAny<DataRecord>(), record.ETag),
                Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntModifyARecordIfTheEtagDoesntMatch()
        {
            // Arrange
            var record = new DataRecord();
            record.SetETag("foo");
            this.cosmosDbSql.Setup(x => x.UpsertAsync(this.cosmosDbSqlClient.Object, this.storageConfig, It.IsAny<DataRecord>(), record.ETag))
                .Throws(BuildDocumentClientException(HttpStatusCode.PreconditionFailed));

            // Act
            Assert.ThrowsAsync<ConflictingResourceException>(
                async () => await this.target.UpsertAsync(record)).CompleteOrTimeout();

            // Assert
            this.cosmosDbSql.Verify(x => x.UpsertAsync(this.cosmosDbSqlClient.Object, this.storageConfig, It.IsAny<DataRecord>(), record.ETag),
                Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanDeleteRecords()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();

            // Act
            this.target.DeleteAsync(id).CompleteOrTimeout();

            // Assert
            this.cosmosDbSql.Verify(x => x.DeleteAsync(this.cosmosDbSqlClient.Object, this.storageConfig, id),
                Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntThrowWhenDeletingARecordThatDoesntExist()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            this.cosmosDbSql.Setup(x => x.DeleteAsync(this.cosmosDbSqlClient.Object, this.storageConfig, id))
                .Throws(BuildDocumentClientException(HttpStatusCode.NotFound));

            // Act - no exception occurs
            this.target.DeleteAsync(id).CompleteOrTimeout();

            // Assert
            this.cosmosDbSql.Verify(x => x.DeleteAsync(this.cosmosDbSqlClient.Object, this.storageConfig, id),
                Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDeletesMulti()
        {
            // Arrange
            var numToDelete = 5;
            var idsToDelete = new List<string>();
            for (var i = 0; i < numToDelete; i++) idsToDelete.Add(i.ToString());

            // Act, Assert
            this.target.DeleteMultiAsync(idsToDelete).CompleteOrTimeout();

            this.cosmosDbSql.Verify(
                x => x.DeleteAsync(
                    It.IsAny<IDocumentClient>(),
                    It.IsAny<Config>(),
                    It.IsAny<string>()),
                Times.Exactly(numToDelete));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItTriesToLockRecordThatIsNotExpired()
        {
            // Arrange
            var id = "123";
            var ownerId = "foo";
            var ownerType = "bar";
            var lockDurationSeconds = 5;

            IResourceResponse<Document> record = BuildResponseWithDocument(
                id: id,
                lockOwnerId: ownerId,
                lockOwnerType: ownerType,
                lockExpirationUtcMsecs: Now + testOffsetMs,
                expired: false);
            this.cosmosDbSql.Setup(x => x.ReadAsync(It.IsAny<IDocumentClient>(), this.storageConfig, id))
                .ReturnsAsync(record);

            // Act
            bool result = this.target.TryToLockAsync(id, ownerId, ownerType, lockDurationSeconds)
                .CompleteOrTimeout().Result;

            // Assert
            Assert.True(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItTriesToUnlockRecord()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var ownerId = Guid.NewGuid().ToString();
            var ownerType = Guid.NewGuid().ToString();
            IResourceResponse<Document> record = BuildResponseWithDocument(
                id: id,
                lockOwnerId: ownerId,
                lockOwnerType: ownerType,
                lockExpirationUtcMsecs: Now + testOffsetMs,
                expired: false);
            this.cosmosDbSql.Setup(x => x.ReadAsync(It.IsAny<IDocumentClient>(), this.storageConfig, id))
                .ReturnsAsync(record);

            // Act
            this.target.TryToUnlockAsync(id, ownerId, ownerType).CompleteOrTimeout();

            // Assert
            this.cosmosDbSql.Verify(
                x => x.UpsertAsync(
                    It.IsAny<IDocumentClient>(),
                    It.IsAny<Config>(),
                    It.IsAny<Resource>()
                ), Times.Once());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsFalseWhenUnlockingRecordThatIsLockedByOther()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var ownerId = Guid.NewGuid().ToString();
            var ownerType = Guid.NewGuid().ToString();
            IResourceResponse<Document> record = BuildResponseWithDocument(
                id: id,
                lockOwnerId: ownerId,
                lockOwnerType: ownerType,
                lockExpirationUtcMsecs: Now + testOffsetMs,
                expired: false);
            this.cosmosDbSql.Setup(x => x.ReadAsync(It.IsAny<IDocumentClient>(), this.storageConfig, id))
                .ReturnsAsync(record);

            // Act
            bool result = this.target.TryToUnlockAsync(id, "wrongOwner", ownerType).CompleteOrTimeout().Result;

            // Assert
            Assert.False(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItConvertsTheLockedState()
        {
            // Arrange
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            var ownerId = "foo";
            var ownerType = "bar";

            var unlockedDoc = new Document { Id = id1 };
            unlockedDoc.SetPropertyValue("Data", "");
            unlockedDoc.SetPropertyValue("ExpirationUtcMsecs", Now + testOffsetMs);
            unlockedDoc.SetPropertyValue("LockOwnerId", ownerId);
            unlockedDoc.SetPropertyValue("LockOwnerType", ownerType);
            unlockedDoc.SetPropertyValue("LockExpirationUtcMsecs", Now - testOffsetMs);

            var lockedDoc = new Document { Id = id2 };
            lockedDoc.SetPropertyValue("Data", "");
            lockedDoc.SetPropertyValue("ExpirationUtcMsecs", Now + testOffsetMs);
            lockedDoc.SetPropertyValue("LockOwnerId", ownerId);
            lockedDoc.SetPropertyValue("LockOwnerType", ownerType);
            lockedDoc.SetPropertyValue("LockExpirationUtcMsecs", Now + testOffsetMs);

            this.cosmosDbSql.Setup(x => x.ReadAsync(this.cosmosDbSqlClient.Object, this.storageConfig, id1))
                .ReturnsAsync(new ResourceResponse<Document>(unlockedDoc));

            this.cosmosDbSql.Setup(x => x.ReadAsync(this.cosmosDbSqlClient.Object, this.storageConfig, id2))
                .ReturnsAsync(new ResourceResponse<Document>(lockedDoc));

            // Act
            IDataRecord unlockedResult = this.target.GetAsync(id1).CompleteOrTimeout().Result;
            IDataRecord lockedResult = this.target.GetAsync(id2).CompleteOrTimeout().Result;

            // Assert
            Assert.Equal(id1, unlockedResult.GetId());
            Assert.Equal(id2, lockedResult.GetId());
            Assert.False(unlockedResult.IsExpired());
            Assert.False(lockedResult.IsExpired());
            Assert.True(lockedResult.IsLocked());
            Assert.False(unlockedResult.IsLocked());
        }

        // Helper to build responses returned by the SDK
        private static IResourceResponse<Document> BuildResponseWithDocument(
            string id = "id",
            string eTag = "-",
            string data = "{}",
            string lockOwnerId = "",
            string lockOwnerType = "",
            long lockExpirationUtcMsecs = 0,
            bool expired = false)
        {
            var document = new Document { Id = id };

            var expiration = !expired ? DataRecord.NEVER : Now - testOffsetMs;

            document.SetPropertyValue("_etag", eTag);
            document.SetPropertyValue("Data", data);
            document.SetPropertyValue("ExpirationUtcMsecs", expiration);
            document.SetPropertyValue("LastModifiedUtcMsecs", 0);
            document.SetPropertyValue("LockOwnerId", lockOwnerId);
            document.SetPropertyValue("LockOwnerType", lockOwnerType);
            document.SetPropertyValue("LockExpirationUtcMsecs", lockExpirationUtcMsecs);

            return new ResourceResponse<Document>(document);
        }

        // Azure Cosmos DB exception classes are internal or sealed, making tests hard to write
        // This ugly workaround uses reflection to create instances thrown by the SDK
        private static DocumentClientException BuildDocumentClientException(HttpStatusCode code)
        {
            var type = typeof(DocumentClientException);
            return (DocumentClientException) type.Assembly.CreateInstance(
                typeName: type.FullName,
                ignoreCase: true,
                bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { "", null, code, null, "" },
                culture: null,
                activationAttributes: null);
        }
    }
}
