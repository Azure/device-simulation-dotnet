// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Microsoft.Azure.Documents;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.CosmosDbSql;
using Xunit;

namespace Services.Test.Storage
{
    public class StorageRecordTest
    {
        private static readonly long testOffsetMs = 5000;
        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        [Fact]
        public void ItConvertsFromCosmosDbSqlLockedAndNotExpired()
        {
            // Arrange
            var id = "123";
            var data = "fooData";
            var expirationUtcMsecs = Now + testOffsetMs;
            var ownerId = "foo";
            var ownerType = "bar";
            var lockExpirationUtcMsecs = Now + testOffsetMs;

            var document = new Document();
            document.Id = id;
            document.SetPropertyValue("Data", data);
            document.SetPropertyValue("ExpirationUtcMsecs", expirationUtcMsecs);
            document.SetPropertyValue("LockOwnerId", ownerId);
            document.SetPropertyValue("LockOwnerType", ownerType);
            document.SetPropertyValue("LockExpirationUtcMsecs", lockExpirationUtcMsecs);

            // Act
            var target = DataRecord.FromCosmosDbSql(document);

            // Assert
            Assert.Equal(id, target.Id);
            Assert.Equal(data, target.Data);
            Assert.False(target.IsExpired());
            Assert.True(target.IsLocked());
        }

        [Fact]
        public void ItConvertsFromCosmosDbSqlExpiredAndNotLocked()
        {
            // Arrange
            var id = "123";
            var data = "fooData";
            var expirationUtcMsecs = Now;
            var ownerId = "foo";
            var ownerType = "bar";
            var lockExpirationUtcMsecs = Now;

            var document = new Document();
            document.Id = id;
            document.SetPropertyValue("Data", data);
            document.SetPropertyValue("ExpirationUtcMsecs", expirationUtcMsecs);
            document.SetPropertyValue("LockOwnerId", ownerId);
            document.SetPropertyValue("LockOwnerType", ownerType);
            document.SetPropertyValue("LockExpirationUtcMsecs", lockExpirationUtcMsecs);

            // Act
            var target = StorageRecord.FromDocumentDb(document);

            // Adding a small sleep to avoid the test executing the subsequent call(s) to
            // time-related methods in sub-millisecond time.
            Thread.Sleep(1);

            // Assert
            Assert.Equal(id, target.Id);
            Assert.Equal(data, target.Data);
            Assert.True(target.IsExpired());
            Assert.False(target.IsLocked());
        }

        [Fact]
        public void ItConvertsFromCosmosDbSqlRecordLockedAndNotExpired()
        {
            // Arrange
            var id = "123";
            var data = "fooData";
            var expirationUtcMsecs = Now + testOffsetMs;
            var ownerId = "foo";
            var ownerType = "bar";
            var lockExpirationUtcMsecs = Now + testOffsetMs;

            DocumentDbRecord document = new DocumentDbRecord();
            document.Id = id;
            document.Data = data;
            document.ExpirationUtcMsecs = expirationUtcMsecs;
            document.LockOwnerId = ownerId;
            document.LockOwnerType = ownerType;
            document.LockExpirationUtcMsecs = lockExpirationUtcMsecs;

            // Act
            var target = DataRecord.FromCosmosDbSqlRecord(document);

            // Assert
            Assert.Equal(id, target.Id);
            Assert.Equal(data, target.Data);
            Assert.False(target.IsExpired());
            Assert.True(target.IsLocked());
        }

        [Fact]
        public void ItConvertsFromCosmosDbSqlRecordExpiredAndNotLocked()
        {
            // Arrange
            var id = "123";
            var data = "fooData";
            var expirationUtcMsecs = Now;
            var ownerId = "foo";
            var ownerType = "bar";
            var lockExpirationUtcMsecs = Now;

            DocumentDbRecord document = new DocumentDbRecord();
            document.Id = id;
            document.Data = data;
            document.ExpirationUtcMsecs = expirationUtcMsecs;
            document.LockOwnerId = ownerId;
            document.LockOwnerType = ownerType;
            document.LockExpirationUtcMsecs = lockExpirationUtcMsecs;

            // Act
            var target = DataRecord.FromCosmosDbSqlRecord(document);

            // Adding a small sleep to avoid the test executing the subsequent call(s) to
            // time-related methods in sub-millisecond time.
            Thread.Sleep(1);

            // Assert
            Assert.Equal(id, target.Id);
            Assert.Equal(data, target.Data);
            Assert.True(target.IsExpired());
            Assert.False(target.IsLocked());
        }
    }
}
