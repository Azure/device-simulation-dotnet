// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.TableStorage;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.Storage.TableStorage
{
    public class DataRecordTest
    {
        private DataRecord target;

        public DataRecordTest()
        {
            this.target = new DataRecord();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItHasAnId()
        {
            // Arrange
            var value = Guid.NewGuid().ToString();

            // Act
            this.target = new DataRecord(value);

            // Assert
            Assert.Equal(value, this.target.GetId());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItHasAnETag()
        {
            // Arrange
            var value = Guid.NewGuid().ToString();

            // Act
            this.target = new DataRecord();
            this.target.SetETag(value);

            // Assert
            Assert.Equal(value, this.target.ETag);
            Assert.Equal(value, this.target.GetETag());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItHasData()
        {
            // Arrange
            var value = Guid.NewGuid().ToString();

            // Act
            this.target = new DataRecord();
            this.target.SetData(value);

            // Assert
            Assert.Equal(value, this.target.GetData());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanContainMoreThan64KbOfData()
        {
            // Arrange
            var value = new string('*', 100000) + Guid.NewGuid();

            // Act
            this.target = new DataRecord();
            this.target.SetData(value);

            // Assert
            Assert.Equal(value, this.target.GetData());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanContainDataUpTo768Kb()
        {
            // Arrange
            const int UTF16_CHARS_IN_64_KB = 1024 * 32 - 1;
            var value1 = new string('*', UTF16_CHARS_IN_64_KB * 12);
            var value2 = new string('*', UTF16_CHARS_IN_64_KB * 12 + 1);

            // Act
            this.target = new DataRecord();
            this.target.SetData(value1);

            // Assert
            Assert.Equal(value1, this.target.GetData());
            Assert.Throws<ArgumentOutOfRangeException>(() => this.target.SetData(value2));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
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

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
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

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
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

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsTrueForExpiredRecords()
        {
            // Arrange
            this.target.ExpiresInMsecs(0);
            Thread.Sleep(10);

            // Act, Assert
            Assert.True(this.target.IsExpired());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
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

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
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

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
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

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
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
