// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.Storage
{
    public class ConfigTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntHaveADefaultStorageType()
        {
            // Act
            var target = new Config();

            // Assert
            Assert.Equal(Type.Unknown, target.StorageType);
        }
    }
}
