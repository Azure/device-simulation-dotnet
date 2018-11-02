using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.ReadDevicesWrapper
{
    public class DevicesWrapperTest
    {
        private const string IOTHUB_CONNSTRING = @"http://diagnostics";
        private const string documentDbCollection = "devices";
        private const int documentDbPageSize = 1;

        private readonly DevicesWrapper target;
        private Mock<RegistryManager> registryManager;

        public DevicesWrapperTest()
        {
            this.target = new DevicesWrapper();
            this.registryManager = new Mock<RegistryManager>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ShouldReadDevicesCollectionFromDocumentDb()
        {
            // Act
            this.target.GetDevices(this.registryManager.Object, documentDbCollection, documentDbPageSize);

            // Assert - check if the createQuery function is called once
            this.registryManager.Verify(x => x.CreateQuery(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }
    }
}
