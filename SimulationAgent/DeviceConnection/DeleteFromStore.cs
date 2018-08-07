// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Deregister the device from the hub registry
    /// </summary>
    public class DeleteFromStore : IDeviceConnectionLogic
    {
        private const string DEVICES_COLLECTION = "SimulatedDevices";
        private readonly IDevices devices;
        private readonly ILogger log;
        private readonly IStorageAdapterClient storage;
        private string deviceId;
        private IDeviceConnectionActor context;

        public DeleteFromStore(IStorageAdapterClient storage, IDevices devices, ILogger logger)
        {
            this.log = logger;
            this.devices = devices;
            this.storage = storage;
        }

        public void Setup(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel)
        {
            this.context = context;
            this.deviceId = deviceId;
        }

        public async Task RunAsync()
        {
            this.log.Debug("Deleting device from store...", () => new { this.deviceId });

            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                try
                {
                    await this.storage.DeleteAsync(DEVICES_COLLECTION, this.deviceId);
                }
                catch (ResourceNotFoundException)
                {
                    this.log.Debug("Device not found", () => new { this.deviceId });
                }

                var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                this.log.Debug("Deleting device from store", () => new { this.deviceId, timeSpent });

                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.DeleteFromStoreCompleted);
            }
            catch (Exception e)
            {
                this.log.Error("Error while deleting the device from store", () => new { this.deviceId, e });
                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.DeleteFromStoreFailed);
            }
        }
    }
}
