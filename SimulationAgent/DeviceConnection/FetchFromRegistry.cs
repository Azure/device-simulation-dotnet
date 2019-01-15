// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Fetch the device from the registry if available
    /// </summary>
    public class FetchFromRegistry : IDeviceConnectionLogic
    {
        private readonly ILogger log;

        public FetchFromRegistry(ILogger logger)
        {
            this.log = logger;
        }

        public async Task RunAsync(IDeviceConnectionActor deviceContext)
        {
            var deviceId = deviceContext.DeviceId;
            var simulationContext = deviceContext.SimulationContext;

            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            try
            {
                this.log.Debug("Fetching device...", () => new { deviceId });

                var device = await simulationContext.Devices.GetAsync(deviceId);

                if (device != null)
                {
                    deviceContext.Device = device;

                    var timeSpentMsecs = GetTimeSpentMsecs();
                    this.log.Debug("Device found", () => new { timeSpentMsecs, device.Id, device.Enabled });

                    deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.FetchCompleted);
                }
                else
                {
                    var timeSpentMsecs = GetTimeSpentMsecs();
                    this.log.Debug("Device not found", () => new { timeSpentMsecs, deviceId });

                    deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceNotFound);
                }
            }
            catch (ExternalDependencyException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("External dependency error while fetching the device", () => new { timeSpentMsecs, deviceId, e });

                deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.FetchFailed);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Error while fetching the device", () => new { timeSpentMsecs, deviceId, e });

                deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.FetchFailed);
            }
        }
    }
}
