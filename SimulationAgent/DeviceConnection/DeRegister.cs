// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Deregister the device from the hub registry
    /// </summary>
    public class Deregister : IDeviceConnectionLogic
    {
        private readonly ILogger log;

        public Deregister(ILogger logger)
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
                this.log.Debug("De-registering device...", () => new { deviceId });

                await simulationContext.Devices.DeleteAsync(deviceId);

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Debug("Device de-registered", () => new { timeSpentMsecs, deviceId });

                deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceDeregistered);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Error while de-registering the device", () => new { timeSpentMsecs, deviceId, e });

                deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeregisterationFailed);
            }
        }
    }
}
