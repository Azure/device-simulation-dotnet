using Microsoft.Azure.Devices;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub
{
    public interface IDevicesWrapper
    {
        void GetDevices(RegistryManager registryManager, string documentDbCollection, int documentDbPageSize);
    }

    public class DevicesWrapper : IDevicesWrapper
    {
        public void GetDevices(RegistryManager registryManager, string documentDbCollection, int documentDbPageSize)
        {
            registryManager.CreateQuery($"select * from {documentDbCollection}", documentDbPageSize);
        }
    }
}
