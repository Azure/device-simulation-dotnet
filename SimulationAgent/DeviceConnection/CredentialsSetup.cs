// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    /// <summary>
    /// Prepare the device credentials using known settings from the local configuration
    /// </summary>
    public class CredentialsSetup : IDeviceConnectionLogic
    {
        private readonly ILogger log;
        private readonly IInstance instance;
        private string deviceId;
        private IDeviceConnectionActor context;

        public CredentialsSetup(ILogger logger, IInstance instance)
        {
            this.log = logger;
            this.instance = instance;
        }

        public void Init(IDeviceConnectionActor actor, string deviceId, DeviceModel deviceModel)
        {
            this.instance.InitOnce();

            this.context = actor;
            this.deviceId = deviceId;

            this.instance.InitComplete();
        }

        public Task RunAsync()
        {
            this.instance.InitRequired();

            this.log.Debug("Configuring device credentials...", () => new { this.deviceId });
            this.context.Device = this.context.SimulationContext.Devices.GetWithKnownCredentials(this.deviceId);
            this.context.HandleEvent(DeviceConnectionActor.ActorEvents.CredentialsSetupCompleted);
            return Task.CompletedTask;
        }
    }
}
