// Copyright (c) Microsoft. All rights reserved. 

using Microsoft.Azure.Devices;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub
{
    public interface ICheckHubPermissionsWrapper
    {
        void TestReadPermissions(RegistryManager registryManager, string documentDbCollection, int documentDbPageSize);
    }

    public class CheckHubPermissionsWrapper : ICheckHubPermissionsWrapper
    {
        public void TestReadPermissions(RegistryManager registryManager, string documentDbCollection, int documentDbPageSize)
        {
            registryManager.GetJobsAsync();
        }
    }
}
