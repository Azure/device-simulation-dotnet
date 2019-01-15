// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Establish a connection to Azure IoT Hub
    /// </summary>
    public class Connect : IDeviceConnectionLogic
    {
        private readonly ILogger log;
        private int connectedDevices = 0;

        public Connect(ILogger logger)
        {
            this.log = logger;
        }

        public async Task RunAsync(IDeviceConnectionActor deviceContext)
        {
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            var deviceId = deviceContext.DeviceId;
            var deviceModel = deviceContext.DeviceModel;
            var simulationContext = deviceContext.SimulationContext;

            try
            {
                // Ensure pending task are stopped
                deviceContext.DisposeClient();

                deviceContext.Client = simulationContext.Devices.GetClient(deviceContext.Device, deviceModel.Protocol);

                await deviceContext.Client.ConnectAsync();
                await deviceContext.Client.RegisterMethodsForDeviceAsync(deviceModel.CloudToDeviceMethods, deviceContext.DeviceState, deviceContext.DeviceProperties, deviceContext.ScriptInterpreter);
                await deviceContext.Client.RegisterDesiredPropertiesUpdateAsync(deviceContext.DeviceProperties);

                var counter = Interlocked.Increment(ref connectedDevices);
                if (counter % 100 == 0)
                {
                    this.log.Info("Connected devices count", () => new { counter });
                }

                // this.log.Info("Device connected", () => new { timeSpentMsecs = GetTimeSpentMsecs(), deviceId });
                deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.Connected);
            }
            catch (DeviceAuthFailedException e)
            {
                this.log.Error("Invalid connection credentials", () => new { timeSpentMsecs = GetTimeSpentMsecs(), deviceId, e });
                deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.AuthFailed);
            }
            catch (DeviceNotFoundException e)
            {
                this.log.Error("Device not found", () => new { timeSpentMsecs = GetTimeSpentMsecs(), deviceId, e });
                deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.DeviceNotFound);
            }
            catch (Exception e)
            {
                this.log.Error("Connection error", () => new { timeSpentMsecs = GetTimeSpentMsecs(), deviceId, e });
                deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.ConnectionFailed);
            }
        }
    }
}
