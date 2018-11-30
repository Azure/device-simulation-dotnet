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
    /// Fetch the device from the registry if available
    /// </summary>
    public class FetchFromRegistry : IDeviceConnectionLogic
    {
        private readonly ILogger log;
        private readonly IInstance instance;
        private string deviceId;
        private IDeviceConnectionActor deviceContext;
        private ISimulationContext simulationContext;

        public FetchFromRegistry(ILogger logger, IInstance instance)
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

            this.log.Debug("Fetching device...", () => new { this.deviceId });

            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            try
            {
                var device = await this.simulationContext.Devices.GetAsync(this.deviceId);

                if (device != null)
                {
                    this.deviceContext.Device = device;

                    var timeSpentMsecs = GetTimeSpentMsecs();
                    this.log.Debug("Device found",
                        () => new { timeSpentMsecs, device.Id, device.Enabled });

                    this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.FetchCompleted);
                }
                else
                {
                    var timeSpentMsecs = GetTimeSpentMsecs();
                    this.log.Debug("Device not found",
                        () => new { timeSpentMsecs, this.deviceId });

                    this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceNotFound);
                }
            }
            catch (ExternalDependencyException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("External dependency error while fetching the device",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.FetchFailed);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Error while fetching the device",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.FetchFailed);
            }
        }
    }
}
