// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Prepare the device credentials using known settings from the local configuration
    /// </summary>
    public class CredentialsSetup : IDeviceConnectionLogic
    {
        private readonly ILogger log;

        public CredentialsSetup(ILogger logger)
        {
            this.log = logger;
        }

        public Task RunAsync(IDeviceConnectionActor deviceContext)
        {
            this.log.Debug("Configuring device credentials...", () => new { deviceContext.DeviceId });
            deviceContext.Device = deviceContext.SimulationContext.Devices.GetWithKnownCredentials(deviceContext.DeviceId);
            deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.CredentialsSetupCompleted);

            return Task.CompletedTask;
        }
    }
}
