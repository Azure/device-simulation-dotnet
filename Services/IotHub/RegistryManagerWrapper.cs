// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub
{
    /**
     * Shim interface to allow mocking the IoT Hub SDK registry manager
     * static methods in the unit tests.
     */
    public interface IRegistryManager
    {
        void Init(string connString);
        void ValidateConnectionString(string connString);
        Task<BulkRegistryOperationResult> AddDevices2Async(IEnumerable<Device> devices);
        Task<BulkRegistryOperationResult> RemoveDevices2Async(IEnumerable<Device> devices, bool forceRemove);
        Task OpenAsync();
        Task CloseAsync();
        Task<Device> AddDeviceAsync(Device device);
        Task RemoveDeviceAsync(string deviceId);
        Task<Device> GetDeviceAsync(string deviceId);
        Task UpdateTwinAsync(string deviceId, Twin twinPatch, string eTag);
        Task<JobProperties> ImportDevicesAsync(string containerUri, string inputBlobName);
        Task<JobProperties> GetJobAsync(string jobId);
        void Dispose();
    }

    /**
     * Shim class to allow mocking the IoT Hub SDK registry manager
     * static methods in the unit tests. No logic here, only forwarding
     * methods to the actual class in the SDK.
     */
    public class RegistryManagerWrapper : IRegistryManager, IDisposable
    {
        private readonly IInstance instance;

        private RegistryManager registry;

        public RegistryManagerWrapper(IInstance instance)
        {
            this.instance = instance;
            this.registry = null;
        }

        public void Init(string connString)
        {
            this.instance.InitOnce();
            this.registry = RegistryManager.CreateFromConnectionString(connString);
            this.instance.InitComplete();
        }

        public void ValidateConnectionString(string connString)
        {
            var test = RegistryManager.CreateFromConnectionString(connString);
            test.Dispose();
        }

        public async Task<BulkRegistryOperationResult> AddDevices2Async(
            IEnumerable<Device> devices)
        {
            this.instance.InitRequired();
            return await this.registry.AddDevices2Async(devices, CancellationToken.None);
        }

        public async Task RemoveDeviceAsync(string deviceId)
        {
            this.instance.InitRequired();
            await this.registry.RemoveDeviceAsync(deviceId);
        }

        public async Task<BulkRegistryOperationResult> RemoveDevices2Async(
            IEnumerable<Device> devices,
            bool forceRemove)
        {
            this.instance.InitRequired();
            return await this.registry.RemoveDevices2Async(devices, forceRemove, CancellationToken.None);
        }

        public async Task OpenAsync()
        {
            this.instance.InitRequired();
            await this.registry.OpenAsync();
        }

        public async Task CloseAsync()
        {
            this.instance.InitRequired();
            await this.registry.CloseAsync();
        }

        public async Task<Device> AddDeviceAsync(Device device)
        {
            this.instance.InitRequired();
            return await this.registry.AddDeviceAsync(device);
        }

        public async Task<Device> GetDeviceAsync(string deviceId)
        {
            this.instance.InitRequired();
            return await this.registry.GetDeviceAsync(deviceId);
        }

        public async Task UpdateTwinAsync(string deviceId, Twin twinPatch, string eTag)
        {
            this.instance.InitRequired();
            await this.registry.UpdateTwinAsync(deviceId, twinPatch, eTag);
        }

        public async Task<JobProperties> ImportDevicesAsync(string containerUri, string inputBlobName)
        {
            this.instance.InitRequired();
            return await this.registry.ImportDevicesAsync(containerUri, containerUri, inputBlobName);
        }

        public Task<JobProperties> GetJobAsync(string jobId)
        {
            this.instance.InitRequired();
            return this.registry.GetJobAsync(jobId);
        }

        public void Dispose()
        {
            this.registry?.Dispose();
        }
    }
}
