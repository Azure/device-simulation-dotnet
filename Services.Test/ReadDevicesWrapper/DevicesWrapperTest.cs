// Copyright (c) Microsoft. All rights reserved. 

using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.ReadDevicesWrapper
{
    public class DevicesWrapperTest
    {
        private const string IOTHUB_CONNSTRING = @"http://foobar";
        private const string documentDbCollection = "devices";
        private const int documentDbPageSize = 1;

        private readonly DevicesWrapper target;
        private Mock<RegistryManager> mockRegistryManager;

        public DevicesWrapperTest()
        {
            this.target = new DevicesWrapper();
            this.mockRegistryManager = new Mock<RegistryManager>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ShouldReadDevicesCollectionFromDocumentDb()
        {
            // Act
            this.target.GetDevices(this.mockRegistryManager.Object, documentDbCollection, documentDbPageSize);

            // Assert - check if the createQuery function is called once
            this.mockRegistryManager.Verify(x => x.CreateQuery(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }
    }
}
