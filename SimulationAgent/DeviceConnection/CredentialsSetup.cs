// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Prepare the device credentials using known settings from the local configuration
    /// </summary>
    public class CredentialsSetup : IDeviceConnectionLogic
    {
        private readonly IDevices devices;
        private readonly ILogger log;
        private string deviceId;
        private IDeviceConnectionActor context;

        public CredentialsSetup(IDevices devices, ILogger logger)
        {
            this.log = logger;
            this.devices = devices;
        }

        public void Setup(IDeviceConnectionActor context, string deviceId, DeviceModel deviceModel)
        {
            this.context = context;
            this.deviceId = deviceId;
        }

        public Task RunAsync()
        {
            this.log.Debug("Configuring device credentials...", () => new { this.deviceId });
            this.context.Device = this.devices.GetWithKnownCredentials(this.deviceId);
            this.context.HandleEvent(DeviceConnectionActor.ActorEvents.CredentialsSetupCompleted);
            return Task.CompletedTask;
        }
    }
}
