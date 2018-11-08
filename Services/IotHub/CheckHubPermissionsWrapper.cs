// Copyright (c) Microsoft. All rights reserved. 

using Microsoft.Azure.Devices;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub
{
    public interface ICheckHubPermissionsWrapper
    {
        Task CheckPermissionsAsync(RegistryManager registryManager);
    }

    public class CheckHubPermissionsWrapper : ICheckHubPermissionsWrapper
    {
        public async Task CheckPermissionsAsync(RegistryManager registryManager)
        {
            await registryManager.GetJobsAsync();
        }
    }
}
