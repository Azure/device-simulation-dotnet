// Copyright (c) Microsoft. All rights reserved. 

using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.ReadDevicesWrapper
{
    public class CheckHubPermissionsWrapperTest
    {
        private const string IOTHUB_CONNSTRING = @"http://foobar";
        private const string documentDbCollection = "devices";
        private const int documentDbPageSize = 1;

        private readonly CheckHubPermissionsWrapper target;
        private Mock<RegistryManager> mockRegistryManager;

        public CheckHubPermissionsWrapperTest()
        {
            this.target = new CheckHubPermissionsWrapper();
            this.mockRegistryManager = new Mock<RegistryManager>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ShouldCheckForHubPermissions()
        {
            // Act
            this.target.CheckPermissions(this.mockRegistryManager.Object, documentDbCollection, documentDbPageSize);

            // Assert - check if the createQuery function is called once
            this.mockRegistryManager.Verify(x => x.GetJobsAsync(), Times.Once);
        }
    }
}
