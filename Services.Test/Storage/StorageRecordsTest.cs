// // Copyright (c) Microsoft. All rights reserved.
//
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Net;
// using System.Net.Http.Headers;
// using System.Reflection;
// using System.Threading.Tasks;
// using Microsoft.Azure.Documents;
// using Microsoft.Azure.Documents.Client;
// using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
// using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
// using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
// using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
// using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.CosmosDbSql;
// using Moq;
// using Xunit;
//
// namespace Services.Test.Storage
// {
//     public class StorageRecordsTest
//     {
//         private static readonly TimeSpan testTimeout = TimeSpan.FromSeconds(5);
//         private static readonly long testOffsetMs = 5000;
//         private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
//
//         private Engine target;
//         private Mock<ISDKWrapper> mockCosmosDbSqlWrapper;
//         private Mock<ILogger> mockLogger;
//         private Mock<IInstance> mockInstance;
//         private Mock<IDocumentClient> mockDocumentClient;
//         private Config storageConfig;
//         private readonly Mock<IResourceResponse<Document>> mockStorageDocument;
//
//         public StorageRecordsTest()
//         {
//             this.mockLogger = new Mock<ILogger>();
//             this.mockInstance = new Mock<IInstance>();
//             this.mockDocumentClient = new Mock<IDocumentClient>();
//             this.mockStorageDocument = new Mock<IResourceResponse<Document>>();
//
//             // Set up a CosmosDbSqlWrapper Mock to return the mock storage document
//             this.mockCosmosDbSqlWrapper = new Mock<ISDKWrapper>();
//             this.mockCosmosDbSqlWrapper.Setup(
//                 x => x.GetClientAsync(
//                     It.IsAny<Config>())
//             ).ReturnsAsync(this.mockDocumentClient.Object);
//
//             this.mockCosmosDbSqlWrapper.Setup(
//                 x => x.ReadAsync(
//                     It.IsAny<IDocumentClient>(),
//                     It.IsAny<Config>(),
//                     It.IsAny<string>())
//             ).ReturnsAsync(this.mockStorageDocument.Object);
//
//             this.target = new Engine(
//                 this.mockCosmosDbSqlWrapper.Object,
//                 this.mockLogger.Object,
//                 this.mockInstance.Object);
//
//             this.storageConfig = new Config();
//             this.target.Init(this.storageConfig);
//         }
//
//         [Fact]
//         public void ItReturnsASingleStorageRecordThatHasNotExpired()
//         {
//             // Arrange
//             var id = "123";
//
//             // Mock a storage document that will be returned
//             var document = new Document();
//             document.Id = id;
//             document.SetPropertyValue("ExpirationUtcMsecs", Now + testOffsetMs);
//             this.mockStorageDocument.SetupGet(x => x.Resource).Returns(document);
//
//             // Act
//             var storageRecordTask = this.target.GetAsync(id);
//             storageRecordTask.Wait(testTimeout);
//
//             // Assert
//             Assert.Equal(id, storageRecordTask.Result.Id);
//         }
//
//         [Fact]
//         public void ItThrowsResourceNotFoundForExpiredRecords()
//         {
//             // Arrange
//             var id = "123";
//
//             // Mock a storage document that will be returned
//             var document = new Document();
//             document.Id = id;
//             document.SetPropertyValue("ExpirationUtcMsecs", 0);
//             this.mockStorageDocument.SetupGet(x => x.Resource).Returns(document);
//
//             // Act, Assert
//             var ex = Assert.Throws<AggregateException>(() =>
//             {
//                 Task resultTask = this.target.GetAsync(id);
//                 resultTask.Wait(testTimeout);
//             });
//             Assert.IsType<ResourceNotFoundException>(ex.InnerException);
//         }
//
//         [Fact]
//         public void ItVerifiesThatARecordExists()
//         {
//             // Arrange
//             var id = "123";
//
//             // Mock a storage document that will be returned
//             var document = new Document();
//             document.Id = id;
//             document.SetPropertyValue("ExpirationUtcMsecs", Now + testOffsetMs);
//             this.mockStorageDocument.SetupGet(x => x.Resource).Returns(document);
//
//             // Act
//             var existsAsyncTask = this.target.ExistsAsync(id);
//             existsAsyncTask.Wait(testTimeout);
//
//             // Assert
//             Assert.True(existsAsyncTask.Result);
//         }
//
//         [Fact]
//         public void ItReturnsFalseForRecordsThatDoNotExist()
//         {
//             // Arrange
//             var id = "123";
//
//             // Mock a storage document that will be returned
//             var document = new Document();
//             document.Id = id;
//             document.SetPropertyValue("ExpirationUtcMsecs", 0);
//             this.mockStorageDocument.SetupGet(x => x.Resource).Returns(document);
//
//             // Act
//             var existsAsyncTask = this.target.ExistsAsync(id);
//             existsAsyncTask.Wait(testTimeout);
//
//             // Assert
//             Assert.False(existsAsyncTask.Result);
//         }
//
//         [Fact]
//         public void ItReturnsAllRecords()
//         {
//             // Arrange
//             // Create a collection of test StorageRecords
//             var records = new List<IDataRecord>();
//             records.Add(
//                 new DataRecord
//                 {
//                     Id = "1",
//                     ExpirationUtcMsecs = Now + testOffsetMs
//                 });
//             records.Add(
//                 new DataRecord
//                 {
//                     Id = "2",
//                     ExpirationUtcMsecs = Now + testOffsetMs
//                 });
//
//             // Have the mock CosmosDbSqlWrapper return the test records
//             this.mockCosmosDbSqlWrapper.Setup(
//                 x => x.CreateQuery<IDataRecord>(It.IsAny<IDocumentClient>(), It.IsAny<Config>())
//             ).Returns(
//                 records.AsQueryable().OrderBy(x => x.Id)
//             );
//
//             // Act
//             var recordsTask = this.target.GetAllAsync();
//             recordsTask.Wait(testTimeout);
//
//             // Assert
//             Assert.Equal(records.Count, recordsTask.Result.Count());
//             Assert.Equal("1", recordsTask.Result.ElementAt(0).GetId());
//             Assert.Equal("2", recordsTask.Result.ElementAt(1).GetId());
//         }
//
//         [Fact]
//         public void ItLogsAnErrorWhenADbOperationFails()
//         {
//             // Arrange
//             this.mockCosmosDbSqlWrapper.Setup(
//                 x => x.CreateAsync(It.IsAny<IDocumentClient>(), It.IsAny<Config>(), It.IsAny<IDataRecord>())
//             ).Throws(
//                 new Exception()
//             );
//
//             // Act, Assert
//             Assert.ThrowsAnyAsync<Exception>(
//                     () => this.target.GetAllAsync()
//                 )
//                 .Wait(testTimeout);
//             this.mockLogger.Verify(
//                 x => x.Error(
//                     It.IsAny<string>(),
//                     It.IsAny<Func<object>>(),
//                     It.IsAny<string>(),
//                     It.IsAny<string>(),
//                     It.IsAny<int>()), Times.Once());
//         }
//
//         [Fact]
//         public void ItCreatesRecords()
//         {
//             // Arrange
//             // Mock a storage document that will be returned
//             var document = new Document();
//             document.SetPropertyValue("ExpirationUtcMsecs", 0);
//             document.SetPropertyValue("LockOwnerId", "foo");
//             document.SetPropertyValue("LockOwnerType", "bar");
//
//             this.mockStorageDocument.SetupGet(x => x.Resource).Returns(document);
//
//             this.mockCosmosDbSqlWrapper.Setup(
//                 x => x.CreateAsync(It.IsAny<IDocumentClient>(), It.IsAny<Config>(), It.IsAny<IDataRecord>())
//             ).ReturnsAsync(
//                 this.mockStorageDocument.Object
//             );
//
//             var record = new DataRecord();
//
//             // Act
//             var createTask = this.target.CreateAsync(record);
//             createTask.Wait(testTimeout);
//
//             // Assert
//             Assert.Equal("foo", (createTask.Result as DataRecord).GetCosmosDbSqlRecord().LockOwnerId);
//             Assert.Equal("bar", (createTask.Result as DataRecord).GetCosmosDbSqlRecord().LockOwnerType);
//         }
//
//         [Fact]
//         public void ItThrowsConflictingResourceExceptionWhenCreatingAResourceWithAnIdThatIsInUse()
//         {
//             // Arrange
//             var exception = this.BuildDocumentClientException(HttpStatusCode.Conflict);
//
//             this.mockCosmosDbSqlWrapper.Setup(
//                 x => x.CreateAsync(It.IsAny<IDocumentClient>(), It.IsAny<Config>(), It.IsAny<IDataRecord>())
//             ).ThrowsAsync(
//                 (DocumentClientException) exception
//             );
//
//             // Mock storage record
//             var mockStorageRecord = new Mock<IDataRecord>();
//
//             // Act, Assert
//             Assert.ThrowsAnyAsync<ConflictingResourceException>(
//                     () => this.target.CreateAsync(mockStorageRecord.Object)
//                 )
//                 .Wait(testTimeout);
//         }
//
//         [Fact]
//         public void ItDeletesARecord()
//         {
//             // Arrange
//             var id = "123";
//
//             // Act
//             var deleteTask = this.target.DeleteAsync(id);
//             deleteTask.Wait(testTimeout);
//
//             // Assert
//             this.mockCosmosDbSqlWrapper.Verify(
//                 x => x.DeleteAsync(
//                     It.IsAny<IDocumentClient>(),
//                     It.IsAny<Config>(),
//                     It.IsAny<string>()), Times.Once());
//         }
//
//         [Fact]
//         public void ItDeletesMulti()
//         {
//             // Arrange
//             var numToDelete = 5;
//             var idsToDelete = new List<string>();
//             for (var i = 0; i < numToDelete; i++) idsToDelete.Add(i.ToString());
//
//             // Act, Assert
//             var deleteMultiTask = this.target.DeleteMultiAsync(idsToDelete);
//             deleteMultiTask.Wait(testTimeout);
//
//             this.mockCosmosDbSqlWrapper.Verify(
//                 x => x.DeleteAsync(
//                     It.IsAny<IDocumentClient>(),
//                     It.IsAny<Config>(),
//                     It.IsAny<string>()),
//                 Times.Exactly(numToDelete));
//         }
//
//         [Fact]
//         public void ItTriesToLockRecordThatIsNotExpired()
//         {
//             // Arrange
//             var id = "123";
//             var ownerId = "foo";
//             var ownerType = "bar";
//             var lockDurationSeconds = 5;
//
//             // Mock a storage document that will be returned
//             var document = new Document();
//             document.Id = id;
//             document.SetPropertyValue("ExpirationUtcMsecs", Now + testOffsetMs);
//             document.SetPropertyValue("OwnerId", ownerId);
//             document.SetPropertyValue("OwnerType", ownerType);
//
//             this.mockStorageDocument.SetupGet(x => x.Resource).Returns(document);
//
//             // Act
//             var lockTask = this.target.TryToLockAsync(id, ownerId, ownerType, lockDurationSeconds);
//             lockTask.Wait(testTimeout);
//
//             // Assert
//             Assert.True(lockTask.Result);
//         }
//
//         [Fact]
//         public void ItTriesToUnlockRecord()
//         {
//             // Arrange
//             var id = "123";
//             var ownerId = "foo";
//             var ownerType = "bar";
//
//             // Mock a storage document that will be returned
//             var document = new Document();
//             document.Id = id;
//             document.SetPropertyValue("ExpirationUtcMsecs", Now + testOffsetMs);
//             document.SetPropertyValue("LockExpirationUtcMsecs", Now + testOffsetMs);
//             document.SetPropertyValue("LockOwnerId", ownerId);
//             document.SetPropertyValue("LockOwnerType", ownerType);
//
//             this.mockStorageDocument.SetupGet(x => x.Resource).Returns(document);
//
//             // Act
//             var unlockTask = this.target.TryToUnlockAsync(id, ownerId, ownerType);
//             unlockTask.Wait(testTimeout);
//
//             // Assert
//             this.mockCosmosDbSqlWrapper.Verify(
//                 x => x.UpsertAsync(
//                     It.IsAny<IDocumentClient>(),
//                     It.IsAny<Config>(),
//                     It.IsAny<IDataRecord>()
//                 ), Times.Once());
//         }
//
//         [Fact]
//         public void ItUpserts()
//         {
//             // Arranges
//             // Mock storage record
//             var mockStorageRecord = new Mock<IDataRecord>();
//
//             // Act
//             var upsertTask = this.target.UpsertAsync(mockStorageRecord.Object, "foo");
//
//             // Assert
//             this.mockCosmosDbSqlWrapper.Verify(
//                 x => x.UpsertAsync(
//                     It.IsAny<IDocumentClient>(),
//                     It.IsAny<Config>(),
//                     It.IsAny<IDataRecord>(),
//                     It.IsAny<string>()
//                 ), Times.Once());
//         }
//
//         [Fact]
//         public void ItReturnsFalseWhenUnlockingRecordThatIsLockedByOther()
//         {
//             // Arrange
//             var id = "123";
//             var ownerId = "foo";
//             var ownerType = "bar";
//
//             // Mock a storage document that will be returned
//             var document = new Document();
//             document.Id = id;
//             document.SetPropertyValue("ExpirationUtcMsecs", Now + testOffsetMs);
//             document.SetPropertyValue("LockExpirationUtcMsecs", Now + testOffsetMs);
//             document.SetPropertyValue("LockOwnerId", ownerId);
//             document.SetPropertyValue("LockOwnerType", ownerType);
//
//             this.mockStorageDocument.SetupGet(x => x.Resource).Returns(document);
//
//             // Act
//             var unlockTask = this.target.TryToUnlockAsync(id, "blarg", ownerType);
//             unlockTask.Wait(testTimeout);
//
//             // Assert
//             Assert.False(unlockTask.Result);
//         }
//
//         private DocumentClientException BuildDocumentClientException(HttpStatusCode statusCode)
//         {
//             // Create an error message object
//             var err = new Error
//             {
//                 Id = Guid.NewGuid().ToString(),
//                 Code = "foo",
//                 Message = "bar"
//             };
//
//             // Create a DocumentClientException object
//             var type = typeof(DocumentClientException);
//             var documentClientExceptionObj = type.Assembly.CreateInstance(
//                 type.FullName,
//                 false,
//                 BindingFlags.Instance | BindingFlags.NonPublic,
//                 null,
//                 new object[]
//                 {
//                     err,
//                     (HttpResponseHeaders) null,
//                     statusCode
//                 },
//                 null,
//                 null);
//
//             return (DocumentClientException) documentClientExceptionObj;
//         }
//     }
// }
