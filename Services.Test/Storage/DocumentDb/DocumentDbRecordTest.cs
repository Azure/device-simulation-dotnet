// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.DocumentDb;
using Xunit;

namespace Services.Test.Storage.DocumentDb
{
    public class DocumentDbRecordTest
    {
        private DocumentDbRecord target;
        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public DocumentDbRecordTest()
        {
            this.target = new DocumentDbRecord();
        }

        [Fact]
        public void ItCanUnlockForCurrentOwner()
        {
            // Arrange
            var ownerId = "foo";
            var ownerType = "bar";
            var durationMs = 100;

            // Act
            this.target.Lock(ownerId, ownerType, durationMs);

            // Assert
            Assert.True(this.target.CanUnlock(ownerId, ownerType));
        }

        [Fact]
        public void ItCanUnlockAfterExpiration()
        {
            // Arrange
            var ownerId = "foo";
            var ownerType = "bar";
            var durationSeconds = 1;

            // Act
            this.target.Lock(ownerId, ownerType, durationSeconds);
            Thread.Sleep(durationSeconds * 2 * 1000);

            // Assert
            Assert.True(this.target.CanUnlock("blarg", "bazz"));
        }

        [Fact]
        public void ItThrowsIfADifferentUserUnlocks()
        {
            // Arrange
            var ownerId = "foo";
            var ownerType = "bar";
            var durationSeconds = 5;

            // Act
            this.target.Lock(ownerId, ownerType, durationSeconds);
            var ex = Record.Exception(() => this.target.Unlock("blarg", "bazz"));

            // Assert
            Assert.IsType<ResourceIsLockedByAnotherOwnerException>(ex);
        }

        [Fact]
        public void ItReturnsTrueForExpiredRecords()
        {
            // Arrange
            this.target.ExpiresInMsecs(0);
            Thread.Sleep(10);

            // Act, Assert
            Assert.True(this.target.IsExpired());
        }

        [Fact]
        public void ItCanLockRecords()
        {
            // Arrange
            var ownerId = "foo";
            var ownerType = "bar";
            var durationMs = 100;

            // Act
            this.target.Lock(ownerId, ownerType, durationMs);

            // Assert
            Assert.True(this.target.IsLocked());
            Assert.True(this.target.IsLockedBy(ownerId, ownerType));
        }

        [Fact]
        public void ItCanUnlockRecords()
        {
            // Arrange
            var ownerId = "foo";
            var ownerType = "bar";
            var durationMs = 100;

            // Act
            this.target.Lock(ownerId, ownerType, durationMs);
            this.target.Unlock(ownerId, ownerType);
            
            // Assert
            Assert.False(this.target.IsLocked());
        }

        [Fact]
        public void ItCorrectlyReportsALockByOther()
        {
            // Arrange
            var ownerId = "foo";
            var ownerType = "bar";
            var durationMs = 1000;

            // Act
            this.target.Lock(ownerId, ownerType, durationMs);

            // Assert
            Assert.True(this.target.IsLockedByOthers("blarg","bazz"));
        }

        [Fact]
        public void ItCorrectlyReportsLockedBy()
        {
            // Arrange
            var ownerId = "foo";
            var ownerType = "bar";
            var durationMs = 1000;

            // Act
            this.target.Lock(ownerId, ownerType, durationMs);

            // Assert
            Assert.True(this.target.IsLockedBy(ownerId, ownerType));
        }
    }
}
