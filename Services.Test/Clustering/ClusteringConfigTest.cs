// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.Clustering
{
    public class ClusteringConfigTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItSupportsOnlyValid_CheckInterval()
        {
            // Arrange
            const int MIN = 1000;
            const int MAX = 300000;
            var target = new ClusteringConfig();

            // Act - no exceptions here
            target.CheckIntervalMsecs = MIN;
            target.CheckIntervalMsecs = MAX;

            // Assert
            Assert.Throws<InvalidConfigurationException>(() => target.CheckIntervalMsecs = MIN - 1);
            Assert.Throws<InvalidConfigurationException>(() => target.CheckIntervalMsecs = MAX + 1);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItSupportsOnlyValid_NodeRecordMaxAge()
        {
            // Arrange
            const int MIN = 10000;
            const int MAX = 600000;
            var target = new ClusteringConfig();

            // Act - no exceptions here
            target.NodeRecordMaxAgeMsecs = MIN;
            target.NodeRecordMaxAgeMsecs = MAX;

            // Assert
            Assert.Throws<InvalidConfigurationException>(() => target.NodeRecordMaxAgeMsecs = MIN - 1);
            Assert.Throws<InvalidConfigurationException>(() => target.NodeRecordMaxAgeMsecs = MAX + 1);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItAllowsToSet_NodeRecordMaxAge_InMsecsAndRetrieveInSecsAndMsecs()
        {
            // Arrange
            var target = new ClusteringConfig();

            // Act
            target.NodeRecordMaxAgeMsecs = 20100;

            // Assert - Note: expect rounded up (ceiling) values
            Assert.Equal(20100, target.NodeRecordMaxAgeMsecs);
            Assert.Equal(21, target.NodeRecordMaxAgeSecs);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItSupportsOnlyValid_MasterLockDuration()
        {
            // Arrange
            const int MIN = 10000;
            const int MAX = 300000;
            var target = new ClusteringConfig();

            // Act - no exceptions here
            target.MasterLockDurationMsecs = MIN;
            target.MasterLockDurationMsecs = MAX;

            // Assert
            Assert.Throws<InvalidConfigurationException>(() => target.MasterLockDurationMsecs = MIN - 1);
            Assert.Throws<InvalidConfigurationException>(() => target.MasterLockDurationMsecs = MAX + 1);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItAllowsToSet_MasterLockDuration_InMsecsAndRetrieveInSecsAndMsecs()
        {
            // Arrange
            var target = new ClusteringConfig();

            // Act
            target.MasterLockDurationMsecs = 20100;

            // Assert - Note: expect rounded up (ceiling) values
            Assert.Equal(20100, target.MasterLockDurationMsecs);
            Assert.Equal(21, target.MasterLockDurationSecs);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItSupportsOnlyValid_MaxPartitionSize()
        {
            // Arrange
            const int MIN = 1;
            const int MAX = 10000;
            var target = new ClusteringConfig();

            // Act - no exceptions here
            target.MaxPartitionSize = MIN;
            target.MaxPartitionSize = MAX;

            // Assert
            Assert.Throws<InvalidConfigurationException>(() => target.MaxPartitionSize = MIN - 1);
            Assert.Throws<InvalidConfigurationException>(() => target.MaxPartitionSize = MAX + 1);
        }
    }
}
