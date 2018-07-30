// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Fetch the device from the registry if available
    /// </summary>
    public class FetchFromRegistry : IDeviceConnectionLogic
    {
        private readonly ILogger log;
        private IDeviceConnectionActor deviceContext;
        private ISimulationContext simulationContext;
        private string deviceId;

        public FetchFromRegistry(ILogger logger)
        {
            this.log = logger;
        }

        public void Init(IDeviceConnectionActor deviceContext, string deviceId, DeviceModel deviceModel)
        {
            this.deviceContext = deviceContext;
            this.simulationContext = deviceContext.SimulationContext;
            this.deviceId = deviceId;
        }

        public async Task RunAsync()
        {
            this.log.Debug("Fetching device...", () => new { this.deviceId });
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                var device = await this.simulationContext.Devices.GetAsync(this.deviceId);

                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                if (device != null)
                {
                    this.deviceContext.Device = device;
                    this.log.Debug("Device found", () => new { timeSpentMsecs, device.Id, device.Enabled });
                    this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.FetchCompleted);
                }
                else
                {
                    this.log.Debug("Device not found", () => new { timeSpentMsecs, this.deviceId });
                    this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceNotFound);
                }
            }
            catch (ExternalDependencyException e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("External dependency error while fetching the device", () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.FetchFailed);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Error while fetching the device", () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.FetchFailed);
            }
        }
    }
}
