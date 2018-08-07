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
    public class AddToStore : IDeviceConnectionLogic
    {
        private const string DEVICES_COLLECTION = "SimulatedDevices";
        private readonly IDevices devices;
        private readonly ILogger log;
        private readonly IStorageAdapterClient storage;
        private string deviceId;
        private IDeviceConnectionActor context;

        public AddToStore(IStorageAdapterClient storage, IDevices devices, ILogger logger)
        {
            this.storage = storage;
            this.devices = devices;
            this.log = logger;
        }

        public void Setup(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel)
        {
            this.context = context;
            this.deviceId = deviceId;
        }

        public async Task RunAsync()
        {
            this.log.Debug("Add device to store...", () => new { this.deviceId });

            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                try
                {
                    await this.storage.GetAsync(DEVICES_COLLECTION, this.deviceId);
                }
                catch (ResourceNotFoundException)
                {
                    await this.storage.CreateAsync(DEVICES_COLLECTION, this.deviceId, this.deviceId);
                }
                
                var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                this.log.Debug("Add device to store", () => new { this.deviceId, timeSpent });

                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.AddToStoreCompleted);
            }
            catch (Exception e)
            {
                this.log.Error("Error while adding device to store", () => new { this.deviceId, e });
                this.context.HandleEvent(DeviceConnectionActor.ActorEvents.AddToStoreFailed);
            }
        }
    }
}
