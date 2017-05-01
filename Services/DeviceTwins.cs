// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceTwins
    {
        Task<IEnumerable<DeviceTwinServiceModel>> GetListAsync();

        Task<DeviceTwinServiceModel> GetAsync(string deviceId);
    }

    public class DeviceTwins : IDeviceTwins
    {
        // Max is 1000
        private const int PageSize = 1000;

        private readonly RegistryManager registry;

        public DeviceTwins(IConfig config)
        {
            this.registry = RegistryManager.CreateFromConnectionString(config.HubConnString);
        }

        public async Task<IEnumerable<DeviceTwinServiceModel>> GetListAsync()
        {
            var result = new List<DeviceTwinServiceModel>();
            var query = this.registry.CreateQuery("SELECT * FROM devices", PageSize);
            while (query.HasMoreResults)
            {
                var page = await query.GetNextAsTwinAsync();
                result.AddRange(page.Select(x => new DeviceTwinServiceModel(x)));
            }

            return result;
        }

        public async Task<DeviceTwinServiceModel> GetAsync(string id)
        {
            var twin = await this.registry.GetTwinAsync(id);
            return twin == null ? null : new DeviceTwinServiceModel(twin);
        }
    }
}
