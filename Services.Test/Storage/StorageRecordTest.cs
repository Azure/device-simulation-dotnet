// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.Documents;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.DocumentDb;
using Xunit;

namespace Services.Test.Storage
{
    public class StorageRecordTest
    {
        private static readonly TimeSpan testTimeout = TimeSpan.FromSeconds(5);
        private static readonly long testOffsetMs = 5000;
        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        [Fact]
        public void ItConvertsFromDocumentDb()
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
            var target = StorageRecord.FromDocumentDb(document);

            // Assert
            Assert.Equal(id, target.Id);
            Assert.Equal(data, target.Data);
            Assert.False(target.IsExpired());
            Assert.True(target.IsLocked());
        }

        [Fact]
        public void ItConvertsFromDocumentDbRecord()
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
            var target = StorageRecord.FromDocumentDbRecord(document);

            // Assert
            Assert.Equal(id, target.Id);
            Assert.Equal(data, target.Data);
            Assert.False(target.IsExpired());
            Assert.True(target.IsLocked());
        }
    }
}
