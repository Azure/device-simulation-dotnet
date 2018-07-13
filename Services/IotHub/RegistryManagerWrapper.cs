// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub
{
    /**
     * Shim interface to allow mocking the IoT Hub SDK registry manager
     * static methods in the unit tests.
     */
    public interface IRegistryManager
    {
        IRegistryManager CreateFromConnectionString(string connString);

        Task<BulkRegistryOperationResult> AddDevices2Async(IEnumerable<Device> devices);

        Task<BulkRegistryOperationResult> RemoveDevices2Async(IEnumerable<Device> devices, bool forceRemove);

        Task OpenAsync();

        Task CloseAsync();

        Task<Device> AddDeviceAsync(Device device);

        Task<Device> GetDeviceAsync(string deviceId);

        Task UpdateTwinAsync(string deviceId, Twin twinPatch, string eTag);

        void Dispose();
    }

    /**
     * Shim class to allow mocking the IoT Hub SDK registry manager
     * static methods in the unit tests. No logic here, only forwarding
     * methods to the actual class in the SDK.
     */
    public class RegistryManagerWrapper : IRegistryManager, IDisposable
    {
        private readonly RegistryManager registry;

        public RegistryManagerWrapper()
        {
            this.registry = null;
        }

        public RegistryManagerWrapper(string connString)
        {
            this.registry = RegistryManager.CreateFromConnectionString(connString);
        }

        public IRegistryManager CreateFromConnectionString(string connString)
        {
            return new RegistryManagerWrapper(connString);
        }

        public async Task<BulkRegistryOperationResult> AddDevices2Async(
            IEnumerable<Device> devices)
        {
            return await this.registry.AddDevices2Async(devices, CancellationToken.None);
        }

        public async Task<BulkRegistryOperationResult> RemoveDevices2Async(
            IEnumerable<Device> devices,
            bool forceRemove)
        {
            return await this.registry.RemoveDevices2Async(devices, forceRemove, CancellationToken.None);
        }

        public async Task OpenAsync()
        {
            await this.registry.OpenAsync();
        }

        public async Task CloseAsync()
        {
            await this.registry.CloseAsync();
        }

        public async Task<Device> AddDeviceAsync(Device device)
        {
            return await this.registry.AddDeviceAsync(device);
        }

        public async Task<Device> GetDeviceAsync(string deviceId)
        {
            return await this.registry.GetDeviceAsync(deviceId);
        }

        public async Task UpdateTwinAsync(string deviceId, Twin twinPatch, string eTag)
        {
            await this.registry.UpdateTwinAsync(deviceId, twinPatch, eTag);
        }

        public void Dispose()
        {
            this.registry.Dispose();
        }
    }
}
