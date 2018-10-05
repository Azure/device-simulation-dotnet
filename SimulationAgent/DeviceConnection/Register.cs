// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
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
        private IDeviceConnectionActor deviceConnectionActor;

        public Register(ILogger logger, IInstance instance)
        {
            this.log = logger;
            this.instance = instance;
        }

        public void Init(IDeviceConnectionActor actor, string deviceId, DeviceModel deviceModel)
        {
            this.instance.InitOnce();

            this.deviceConnectionActor = actor;
            this.deviceId = deviceId;

            this.instance.InitComplete();
        }

        public async Task RunAsync()
        {
            this.instance.InitRequired();

            this.log.Debug("Registering device...", () => new { this.deviceId });
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                var device = await this.deviceConnectionActor.SimulationContext.Devices.CreateAsync(this.deviceId);

                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Debug("Device registered", () => new { timeSpentMsecs, this.deviceId });

                this.deviceConnectionActor.Device = device;
                this.deviceConnectionActor.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceRegistered);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Error while registering the device", () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceConnectionActor.HandleEvent(DeviceConnectionActor.ActorEvents.RegistrationFailed);
            }
        }
    }
}
