// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Deregister the device from the hub registry
    /// </summary>
    public class Deregister : IDeviceConnectionLogic
    {
        private readonly ILogger log;
        private readonly IInstance instance;
        private string deviceId;
        private IDeviceConnectionActor deviceContext;
        private ISimulationContext simulationContext;

        public Deregister(ILogger logger, IInstance instance)
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

            this.log.Debug("De-registering device...", () => new { this.deviceId });

            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            try
            {
                await this.simulationContext.Devices.DeleteAsync(this.deviceId);

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Debug("Device de-registered",
                    () => new { timeSpentMsecs, this.deviceId });

                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceDeregistered);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Error while de-registering the device",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeregisterationFailed);
            }
        }
    }
}
