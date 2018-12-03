// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.CosmosDbSql;
using Xunit;

namespace Services.Test.Storage.CosmosDbSql
{
    public class CosmosDbSqlRecordTest
    {
        private DataRecord target;
        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public CosmosDbSqlRecordTest()
        {
            this.target = new DataRecord();
        }

        [Fact]
        public void ItIsNotLockedAndNotExpiredByDefault()
        {
            // Act
            // Recreate the target, so that we can have confidence
            // that a different test hasn't altered it before this test
            // runs
            this.target = new DataRecord();

            // Assert
            Assert.False(this.target.IsLocked());
            Assert.False(this.target.IsExpired());
        }

        [Fact]
        public void ItCanUnlockForCurrentOwner()
        {
            // Arrange
            var ownerId = "foo";
            var ownerType = "bar";
            var durationSeconds = 100;

            // Act
            this.target.Lock(ownerId, ownerType, durationSeconds);

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
            // TODO: isolate this test from the actual CPU clock (i.e. find
            // a way to redefine what "now" means), so that we don't slow
            // down the build tests, etc.
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

            // Assert
            Assert.Throws<ResourceIsLockedByAnotherOwnerException>(
                () => this.target.Unlock("blarg", "bazz"));
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
            var durationSeconds = 100;

            // Act
            this.target.Lock(ownerId, ownerType, durationSeconds);

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
            var durationSeconds = 100;

            // Act
            this.target.Lock(ownerId, ownerType, durationSeconds);
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
            var durationSeconds = 1000;

            // Act
            this.target.Lock(ownerId, ownerType, durationSeconds);

            // Assert
            Assert.True(this.target.IsLockedByOthers("blarg", "bazz"));
        }

        [Fact]
        public void ItCorrectlyReportsLockedBy()
        {
            // Arrange
            var ownerId = "foo";
            var ownerType = "bar";
            var durationSeconds = 1000;

            // Act
            this.target.Lock(ownerId, ownerType, durationSeconds);

            // Assert
            Assert.True(this.target.IsLockedBy(ownerId, ownerType));
        }
    }
}
