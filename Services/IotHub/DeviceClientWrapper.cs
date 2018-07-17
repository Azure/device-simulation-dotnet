// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub
{
    public interface IDeviceClientWrapper
    {
        uint OperationTimeoutInMilliseconds { get; set; }
        IDeviceClientWrapper CreateFromConnectionString(string connectionString, TransportType transportType);
        Task OpenAsync();
        Task CloseAsync();
        Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties);
        Task SendEventAsync(Message message);
        Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext);
        void DisableRetryPolicy();
        Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext);
        void Dispose();
    }

    /// <summary>
    /// Wrap the SDK device client class, to allow unit testing
    /// </summary>
    public class DeviceClientWrapper : IDeviceClientWrapper, IDisposable
    {
        private Azure.Devices.Client.DeviceClient internalClient;

        public uint OperationTimeoutInMilliseconds
        {
            get => this.internalClient.OperationTimeoutInMilliseconds;
            set => this.internalClient.OperationTimeoutInMilliseconds = value;
        }

        public IDeviceClientWrapper CreateFromConnectionString(string connectionString, TransportType transportType)
        {
            var sdkClient = Azure.Devices.Client.DeviceClient.CreateFromConnectionString(connectionString, transportType);
            return this.WrapSdkClient(sdkClient);
        }

        public Task OpenAsync()
        {
            return this.internalClient.OpenAsync();
        }

        public Task CloseAsync()
        {
            return this.internalClient.CloseAsync();
        }

        public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties)
        {
            return this.internalClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        public Task SendEventAsync(Message message)
        {
            return this.internalClient.SendEventAsync(message);
        }

        public Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext)
        {
            return this.internalClient.SetDesiredPropertyUpdateCallbackAsync(callback, userContext);
        }

        public void DisableRetryPolicy()
        {
            this.internalClient.SetRetryPolicy(new NoRetry());
        }

        public Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext)
        {
            return this.internalClient.SetMethodHandlerAsync(methodName, methodHandler, userContext);
        }

        public void Dispose()
        {
            this.internalClient?.Dispose();
        }

        private IDeviceClientWrapper WrapSdkClient(Azure.Devices.Client.DeviceClient sdkClient)
        {
            return new DeviceClientWrapper { internalClient = sdkClient };
        }
    }
}
