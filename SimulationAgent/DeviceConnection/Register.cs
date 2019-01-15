// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Register the device in the hub registry
    /// </summary>
    public class Register : IDeviceConnectionLogic
    {
        private readonly ILogger log;

        public Register(ILogger logger)
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
                this.log.Debug("Registering device...", () => new { deviceId });

                var device = await simulationContext.Devices.CreateAsync(deviceId);

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Debug("Device registered", () => new { timeSpentMsecs, deviceId });

                deviceContext.Device = device;
                deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceRegistered);
            }
            catch (TotalDeviceCountQuotaExceededException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Error while registering the device, quota exceeded", () => new { timeSpentMsecs, deviceId, e });

                deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceQuotaExceeded);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Error while registering the device", () => new { timeSpentMsecs, deviceId, e });

                deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.RegistrationFailed);
            }
        }
    }
}
