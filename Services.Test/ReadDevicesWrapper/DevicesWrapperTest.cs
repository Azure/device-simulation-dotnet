using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.ReadDevicesWrapper
{
    public class DevicesWrapperTest
    {
        private readonly DevicesWrapper target;
        private const string IOTHUB_CONNSTRING = @"http://diagnostics";

        private Mock<RegistryManager> registryManager;

        private const string documentDbCollection = "devices";
        const int documentDbPageSize = 1;

        public DevicesWrapperTest()
        {
            this.registryManager = new Mock<RegistryManager>();
            this.target = new DevicesWrapper();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReadDevices()
        {
            this.target.GetDevices(this.registryManager.Object, documentDbCollection, documentDbPageSize);

            this.registryManager.Verify(x => x.CreateQuery(It.IsAny<string>(), It.IsAny<int>()), Times.Once);

        }
    }
}
