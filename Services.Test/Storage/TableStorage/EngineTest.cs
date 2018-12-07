// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.TableStorage;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.Storage.TableStorage
{
    public class EngineTest
    {
        private readonly Engine target;

        private readonly Mock<IFactory> factory;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IInstance> instance;
        private readonly Mock<ISDKWrapper> tableStorage;
        private readonly Mock<CloudTableClient> tableStorageClient;
        private readonly Mock<CloudTable> tableStorageTable;

        private readonly Config storageConfig;

        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private static readonly long testOffsetMs = 100000;

        public EngineTest()
        {
            this.factory = new Mock<IFactory>();

            this.storageConfig = new Config { TableStorageConnString = "cs", TableStorageTableName = "t" };

            // Mock storage wrapper and client
            this.tableStorage = new Mock<ISDKWrapper>();

            var credentials = new StorageCredentials("acct", "MTIzNDU2");
            this.tableStorageClient = new Mock<CloudTableClient>(
                MockBehavior.Strict, new Uri("http://host"), credentials);
            this.tableStorageTable = new Mock<CloudTable>(
                MockBehavior.Strict, new Uri("http://host"), credentials);
            this.tableStorage.Setup(x => x.CreateCloudTableClient(this.storageConfig))
                .Returns(this.tableStorageClient.Object);
            this.tableStorage.Setup(x => x.GetTableReferenceAsync(this.tableStorageClient.Object, this.storageConfig))
                .ReturnsAsync(this.tableStorageTable.Object);

            this.logger = new Mock<ILogger>();
            this.instance = new Mock<IInstance>();
            this.factory.Setup(x => x.Resolve<ISDKWrapper>()).Returns(this.tableStorage.Object);
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
            const string ID = "qwe";
            this.tableStorage.Setup(x => x.RetrieveAsync(this.tableStorageTable.Object, ID))
                .Throws(BuildStorageException(HttpStatusCode.NotFound));

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
            TableResult response1 = BuildResponseWithContent(id: ID_NEW, expired: false);
            TableResult response2 = BuildResponseWithContent(id: ID_OLD, expired: true);
            this.tableStorage.Setup(x => x.RetrieveAsync(It.IsAny<CloudTable>(), ID_NEW))
                .ReturnsAsync(response1);
            this.tableStorage.Setup(x => x.RetrieveAsync(It.IsAny<CloudTable>(), ID_OLD))
                .ReturnsAsync(response2);

            // Act
            this.target.GetAsync(ID_NEW).CompleteOrTimeout();
            Assert.ThrowsAsync<ResourceNotFoundException>(
                async () => await this.target.GetAsync(ID_OLD)).CompleteOrTimeout();

            // Assert
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                o => o.OperationType == TableOperationType.Delete && ((DataRecord) o.Entity).GetId() == ID_NEW)), Times.Never);
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                o => o.OperationType == TableOperationType.Delete && ((DataRecord) o.Entity).GetId() == ID_OLD)), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDeletesExpiredRecordsOnExistsCheck()
        {
            // Arrange
            const string ID_NEW = "new";
            const string ID_OLD = "old";
            TableResult response1 = BuildResponseWithContent(id: ID_NEW, expired: false);
            TableResult response2 = BuildResponseWithContent(id: ID_OLD, expired: true);
            this.tableStorage.Setup(x => x.RetrieveAsync(It.IsAny<CloudTable>(), ID_NEW))
                .ReturnsAsync(response1);
            this.tableStorage.Setup(x => x.RetrieveAsync(It.IsAny<CloudTable>(), ID_OLD))
                .ReturnsAsync(response2);

            // Act
            this.target.ExistsAsync(ID_NEW).CompleteOrTimeout();
            this.target.ExistsAsync(ID_OLD).CompleteOrTimeout();

            // Assert
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                o => o.OperationType == TableOperationType.Delete && ((DataRecord) o.Entity).GetId() == ID_NEW)), Times.Never);
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                o => o.OperationType == TableOperationType.Delete && ((DataRecord) o.Entity).GetId() == ID_OLD)), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsWhenAGenericExceptionHappens()
        {
            // Arrange
            this.tableStorage.Setup(x => x.RetrieveAsync(It.IsAny<CloudTable>(), It.IsAny<string>()))
                .Throws<SomeException>();
            this.tableStorage.Setup(x => x.ExecuteQuerySegmentedAsync(It.IsAny<CloudTable>(), It.IsAny<TableQuery<DataRecord>>(), It.IsAny<TableContinuationToken>()))
                .Throws<SomeException>();
            this.tableStorage.Setup(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.IsAny<TableOperation>()))
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
            TableResult response1 = BuildResponseWithContent(id: ID_NEW, expired: false);
            TableResult response2 = BuildResponseWithContent(id: ID_OLD, expired: true);
            this.tableStorage.Setup(x => x.RetrieveAsync(It.IsAny<CloudTable>(), ID_NEW))
                .ReturnsAsync(response1);
            this.tableStorage.Setup(x => x.RetrieveAsync(It.IsAny<CloudTable>(), ID_OLD))
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
            var records = new List<DataRecord>
            {
                new DataRecord("old1") { ExpirationUtcMsecs = Now - testOffsetMs },
                new DataRecord("A") { ExpirationUtcMsecs = Now + testOffsetMs },
                new DataRecord("old2") { ExpirationUtcMsecs = Now - testOffsetMs },
                new DataRecord("B") { ExpirationUtcMsecs = Now + testOffsetMs },
                new DataRecord("old3") { ExpirationUtcMsecs = Now - testOffsetMs }
            };
            this.tableStorage.Setup(x => x.ExecuteQuerySegmentedAsync(It.IsAny<CloudTable>(), It.IsAny<TableQuery<DataRecord>>(), It.IsAny<TableContinuationToken>()))
                .ReturnsAsync((records, null));

            // Act
            IEnumerable<IDataRecord> result = this.target.GetAllAsync().CompleteOrTimeout().Result;

            // Assert
            Assert.Equal(2, result.Count());
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                o => o.OperationType == TableOperationType.Delete)), Times.Exactly(3));
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                o => o.OperationType == TableOperationType.Delete && ((DataRecord) o.Entity).GetId() == "old1")), Times.Once);
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                o => o.OperationType == TableOperationType.Delete && ((DataRecord) o.Entity).GetId() == "old2")), Times.Once);
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                o => o.OperationType == TableOperationType.Delete && ((DataRecord) o.Entity).GetId() == "old3")), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanCreateNewRecords()
        {
            // Arrange
            var record = new DataRecord("id");
            TableResult response = BuildResponseWithContent();
            this.tableStorage.Setup(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                    o => o.OperationType == TableOperationType.InsertOrMerge)))
                .ReturnsAsync(response);

            // Act
            this.target.CreateAsync(record).CompleteOrTimeout();

            // Assert
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                o => o.OperationType == TableOperationType.InsertOrMerge)), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsWhenCreatingAConflictingRecord()
        {
            // Arrange
            var record = new DataRecord();
            this.tableStorage.Setup(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.IsAny<TableOperation>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int) HttpStatusCode.Conflict });

            // Act - Assert
            Assert.ThrowsAsync<ConflictingResourceException>(
                async () => await this.target.CreateAsync(record)).CompleteOrTimeout();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanModifyARecordGivenTheRightETag()
        {
            // Arrange
            var eTag = Guid.NewGuid().ToString();
            var record = new DataRecord();
            record.SetETag(eTag);
            TableResult response = BuildResponseWithContent();
            this.tableStorage.Setup(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.IsAny<TableOperation>()))
                .ReturnsAsync(response);

            // Act
            this.target.UpsertAsync(record).CompleteOrTimeout();

            // Assert
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                    o => o.OperationType == TableOperationType.InsertOrMerge && ((DataRecord) o.Entity).GetETag() == eTag)),
                Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntModifyARecordIfTheEtagDoesntMatch()
        {
            // Arrange
            var eTag = Guid.NewGuid().ToString();
            var record = new DataRecord();
            record.SetETag(eTag);
            this.tableStorage.Setup(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.IsAny<TableOperation>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int) HttpStatusCode.PreconditionFailed });

            // Act
            Assert.ThrowsAsync<ConflictingResourceException>(
                async () => await this.target.UpsertAsync(record)).CompleteOrTimeout();

            // Assert
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                    o => o.OperationType == TableOperationType.InsertOrMerge && ((DataRecord) o.Entity).GetETag() == eTag)),
                Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanDeleteRecords()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            this.tableStorage.Setup(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.IsAny<TableOperation>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int) HttpStatusCode.OK });

            // Act
            this.target.DeleteAsync(id).CompleteOrTimeout();

            // Assert
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                    o => o.OperationType == TableOperationType.Delete && ((DataRecord) o.Entity).GetId() == id)),
                Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntThrowWhenDeletingARecordThatDoesntExist()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            this.tableStorage.Setup(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                    o => o.OperationType == TableOperationType.Delete && ((DataRecord) o.Entity).GetId() == id)))
                .Throws(BuildStorageException(HttpStatusCode.NotFound));

            // Act - no exception occurs
            this.target.DeleteAsync(id).CompleteOrTimeout();

            // Assert
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                    o => o.OperationType == TableOperationType.Delete && ((DataRecord) o.Entity).GetId() == id)),
                Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDeletesMulti()
        {
            // Arrange
            var numToDelete = 5;
            var idsToDelete = new List<string>();
            for (var i = 0; i < numToDelete; i++) idsToDelete.Add(i.ToString());
            this.tableStorage.Setup(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                    o => o.OperationType == TableOperationType.Delete)))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int) HttpStatusCode.OK });

            // Act, Assert
            this.target.DeleteMultiAsync(idsToDelete).CompleteOrTimeout();

            this.tableStorage.Verify(
                x => x.ExecuteAsync(
                    It.IsAny<CloudTable>(),
                    It.Is<TableOperation>(o => o.OperationType == TableOperationType.Delete)),
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

            TableResult record = BuildResponseWithContent(
                id: id,
                lockOwnerId: ownerId,
                lockOwnerType: ownerType,
                lockExpirationUtcMsecs: Now + testOffsetMs,
                expired: false);
            this.tableStorage.Setup(x => x.RetrieveAsync(It.IsAny<CloudTable>(), id))
                .ReturnsAsync(record);
            this.tableStorage.Setup(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                    o => o.OperationType == TableOperationType.InsertOrMerge)))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int) HttpStatusCode.OK });

            // Act
            bool result = this.target.TryToLockAsync(id, ownerId, ownerType, lockDurationSeconds)
                .CompleteOrTimeout().Result;

            // Assert
            this.tableStorage.Verify(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                o => o.OperationType == TableOperationType.InsertOrMerge)), Times.Once);
            Assert.True(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItTriesToUnlockRecord()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var ownerId = Guid.NewGuid().ToString();
            var ownerType = Guid.NewGuid().ToString();
            TableResult record = BuildResponseWithContent(
                id: id,
                lockOwnerId: ownerId,
                lockOwnerType: ownerType,
                lockExpirationUtcMsecs: Now + testOffsetMs,
                expired: false);
            this.tableStorage.Setup(x => x.RetrieveAsync(It.IsAny<CloudTable>(), id))
                .ReturnsAsync(record);
            this.tableStorage.Setup(x => x.ExecuteAsync(It.IsAny<CloudTable>(), It.Is<TableOperation>(
                    o => o.OperationType == TableOperationType.InsertOrMerge)))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int) HttpStatusCode.OK });

            // Act
            this.target.TryToUnlockAsync(id, ownerId, ownerType).CompleteOrTimeout();

            // Assert
            this.tableStorage.Verify(
                x => x.ExecuteAsync(
                    It.IsAny<CloudTable>(),
                    It.Is<TableOperation>(o => o.OperationType == TableOperationType.InsertOrMerge)
                ), Times.Once());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsFalseWhenUnlockingRecordThatIsLockedByOther()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var ownerId = Guid.NewGuid().ToString();
            var ownerType = Guid.NewGuid().ToString();
            TableResult record = BuildResponseWithContent(
                id: id,
                lockOwnerId: ownerId,
                lockOwnerType: ownerType,
                lockExpirationUtcMsecs: Now + testOffsetMs,
                expired: false);
            this.tableStorage.Setup(x => x.RetrieveAsync(It.IsAny<CloudTable>(), id))
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

            TableResult unlockedDoc = BuildResponseWithContent(
                id: id1,
                lockOwnerId: ownerId,
                lockOwnerType: ownerType,
                lockExpirationUtcMsecs: Now - testOffsetMs,
                expired: false);

            TableResult lockedDoc = BuildResponseWithContent(
                id: id2,
                lockOwnerId: ownerId,
                lockOwnerType: ownerType,
                lockExpirationUtcMsecs: Now + testOffsetMs,
                expired: false);

            this.tableStorage.Setup(x => x.RetrieveAsync(It.IsAny<CloudTable>(), id1))
                .ReturnsAsync(unlockedDoc);

            this.tableStorage.Setup(x => x.RetrieveAsync(It.IsAny<CloudTable>(), id2))
                .ReturnsAsync(lockedDoc);

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
        private static TableResult BuildResponseWithContent(
            string id = "id",
            string eTag = "-",
            string data = "{}",
            string lockOwnerId = "",
            string lockOwnerType = "",
            long lockExpirationUtcMsecs = 0,
            bool expired = false)
        {
            var record = new DataRecord(id);

            var expiration = !expired ? DataRecord.NEVER : Now - testOffsetMs;

            record.SetETag(eTag);
            record.SetData(data);
            record.ExpirationUtcMsecs = expiration;
            record.LastModifiedUtcMsecs = 0;
            record.LockOwnerId = lockOwnerId;
            record.LockOwnerType = lockOwnerType;
            record.LockExpirationUtcMsecs = lockExpirationUtcMsecs;

            return new TableResult
            {
                Result = record,
                Etag = eTag,
                HttpStatusCode = (int) HttpStatusCode.OK
            };
        }

        private static StorageException BuildStorageException(HttpStatusCode code)
        {
            var res = new RequestResult { HttpStatusCode = (int) code };
            return new StorageException(res, "", null);
        }
    }
}
