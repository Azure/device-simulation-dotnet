// Copyright (c) Microsoft. All rights reserved. 

using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.CheckHubPermissions
{
    public class CheckHubPermissionsWrapperTest
    {
        private readonly CheckHubPermissionsWrapper target;
        private Mock<RegistryManager> mockRegistryManager;

        public CheckHubPermissionsWrapperTest()
        {
            this.target = new CheckHubPermissionsWrapper();
            this.mockRegistryManager = new Mock<RegistryManager>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async void ShouldCheckForHubPermissions()
        {
            // Act
            await this.target.CheckPermissionsAsync(this.mockRegistryManager.Object);

            // Assert - check if the createQuery function is called once
            this.mockRegistryManager.Verify(x => x.GetJobsAsync(), Times.Once);
        }
    }
}
