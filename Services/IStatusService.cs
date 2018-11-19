// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IStatusService
    {
        Task<StatusServiceModel> GetStatusAsync();
    }
}
