// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Register the device in the hub registry
    /// </summary>
    public class Register : IDeviceConnectionLogic
    {
        private readonly ILogger log;
        private readonly IInstance instance;
        private string deviceId;
        private IDeviceConnectionActor deviceContext;
        private ISimulationContext simulationContext;

        public Register(ILogger logger, IInstance instance)
        {
            this.log = logger;
            this.instance = instance;
        }

        public void Init(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel)
        {
            this.instance.InitOnce();

            this.deviceContext = context;
            this.simulationContext = context.SimulationContext;
            this.deviceId = deviceId;

            this.instance.InitComplete();
        }

        public async Task RunAsync()
        {
            this.instance.InitRequired();

            this.log.Debug("Registering device...", () => new { this.deviceId });

            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            try
            {
                var device = await this.simulationContext.Devices.CreateAsync(this.deviceId);

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Debug("Device registered",
                    () => new { timeSpentMsecs, this.deviceId });

                this.deviceContext.Device = device;
                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceRegistered);
            }
            catch (TotalDeviceCountQuotaExceededException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Error while registering the device, quota exceeded",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceQuotaExceeded);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Error while registering the device",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.RegistrationFailed);
            }
        }
    }
}
