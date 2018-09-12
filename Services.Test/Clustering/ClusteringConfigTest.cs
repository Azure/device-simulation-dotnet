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
        public void ItSupportsOnlyValidCheckInterval()
        {
            // Arrange
            var min = 1000;
            var max = 300000;
            var target = new ClusteringConfig();

            // Act - no exceptions here
            target.CheckIntervalMsecs = min;
            target.CheckIntervalMsecs = max;

            // Assert
            Assert.Throws<InvalidConfigurationException>(() => target.CheckIntervalMsecs = min - 1);
            Assert.Throws<InvalidConfigurationException>(() => target.CheckIntervalMsecs = max + 1);
        }
    }
}
