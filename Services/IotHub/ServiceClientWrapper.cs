// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub
{
    public interface IServiceClient
    {
        void Init(string connString);

        Task<ServiceStatistics> GetServiceStatisticsAsync();

        void Dispose();
    }

    public class ServiceClientWrapper : IServiceClient, IDisposable
    {
        private IInstance instance;
        private ServiceClient serviceClient;

        public ServiceClientWrapper(IInstance instance)
        {
            this.instance = instance;
        }

        public void Init(string connString)
        {
            this.instance.InitOnce();
            this.serviceClient = ServiceClient.CreateFromConnectionString(connString);
            this.instance.InitComplete();
        }

        public async Task<ServiceStatistics> GetServiceStatisticsAsync()
        {
            this.instance.InitRequired();
            return await this.serviceClient.GetServiceStatisticsAsync();
        }

        public void Dispose()
        {
            this.ReleaseResources();
        }

        ~ServiceClientWrapper()
        {
            this.ReleaseResources();
        }

        private void ReleaseResources()
        {
            this.serviceClient?.Dispose();
            this.instance = null;
        }
    }
}
