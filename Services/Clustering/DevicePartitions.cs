// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering
{
    public interface IDevicePartitions
    {
        Task CreateAsync(string simId);
        Task<IList<DevicesPartition>> GetAllAsync();
        Task DeleteListAsync(List<string> partitionIds);
    }

    public class DevicePartitions : IDevicePartitions
    {
        public Task CreateAsync(string simId)
        {
            throw new System.NotImplementedException();
        }

        public Task<IList<DevicesPartition>> GetAllAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteListAsync(List<string> partitionIds)
        {
            throw new System.NotImplementedException();
        }
    }
}
