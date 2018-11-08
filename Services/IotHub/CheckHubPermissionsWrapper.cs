// Copyright (c) Microsoft. All rights reserved. 

using Microsoft.Azure.Devices;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub
{
    public interface ICheckHubPermissionsWrapper
    {
        void CheckPermissions(RegistryManager registryManager);
    }

    public class CheckHubPermissionsWrapper : ICheckHubPermissionsWrapper
    {
        public void CheckPermissions(RegistryManager registryManager)
        {
            registryManager.GetJobsAsync();
        }
    }
}
