// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    /// <summary>
    /// Logic executed after Start(), to establish a connection to IoT Hub.
    /// If the connection fails, the actor retries automatically after some
    /// seconds.
    /// </summary>
    public class Connect : IDeviceStatusLogic
    {
        // Retry frequency when failing to connect
        private const int RETRY_FREQUENCY_MSECS = 10000;

        private readonly IDevices devices;
        private readonly ILogger log;
        private string deviceId;
        private IoTHubProtocol protocol;

        // The timer invoking the Run method
        private readonly ITimer timer;

        private readonly IScriptInterpreter scriptInterpreter;

        // Ensure that setup is called once and only once (which helps also detecting thread safety issues)
        private bool setupDone = false;

        private IDeviceActor context;

        public Connect(
            ITimer timer,
            IDevices devices,
            ILogger logger,
            IScriptInterpreter scriptInterpreter)
        {
            this.timer = timer;
            this.log = logger;
            this.devices = devices;
            this.scriptInterpreter = scriptInterpreter;
            this.timer.Setup(this.Run);
        }

        public void Setup(string deviceId, DeviceModel deviceModel, IDeviceActor context)
        {
            if (this.setupDone)
            {
                this.log.Error("Setup has already been invoked, are you sharing this instance with multiple devices?",
                    () => new { this.deviceId });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.setupDone = true;
            this.deviceId = deviceId;
            this.protocol = deviceModel.Protocol;

            this.context = context;
        }

        public void Start()
        {
            this.log.Info("Starting Connect", () => new { this.deviceId });
            this.timer.RunOnce(0);
        }

        public void Stop()
        {
            this.log.Info("Stopping Connect", () => new { this.deviceId });
            this.timer.Cancel();
        }

        public void Run(object context)
        {
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            try
            {
                this.RunInternalAsync().Wait();
            }
            finally
            {
                if (this.context.ActorStatus == Status.Connecting)
                {
                    var passed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                    this.timer.RunOnce(RETRY_FREQUENCY_MSECS - passed);
                }
            }
        }

        private async Task RunInternalAsync()
        {
            this.ValidateSetup();

            var actor = this.context;
            if (actor.CancellationToken.IsCancellationRequested)
            {
                actor.Stop();
                return;
            }

            if (actor.ActorStatus == Status.Connecting)
            {
                this.log.Debug("Connecting...", () => new { this.deviceId });

                try
                {
                    var device = await this.devices.GetOrCreateAsync(this.deviceId, false, actor.CancellationToken);

                    actor.Client = this.devices.GetClient(device, this.protocol, this.scriptInterpreter);
                    await actor.Client.ConnectAsync();

                    // Device Twin properties can be set only over MQTT, so we need a dedicated client
                    // for the bootstrap
                    // TODO: allow to use AMQP https://github.com/Azure/device-simulation-dotnet/issues/92
                    if (actor.Client.Protocol == IoTHubProtocol.MQTT)
                    {
                        actor.BootstrapClient = actor.Client;
                    }
                    else
                    {
                        // bootstrap client is used to call methods and must have a script interpreter associated w/ it.
                        actor.BootstrapClient = this.devices.GetClient(device, IoTHubProtocol.MQTT, this.scriptInterpreter);
                        await actor.BootstrapClient.ConnectAsync();
                    }

                    this.log.Debug("Connection successful", () => new { this.deviceId });

                    actor.MoveNext();
                }
                catch (InvalidConfigurationException e)
                {
                    this.log.Error("Connection failed: unable to initialize the client.",
                        () => new { this.deviceId, e });
                }
                catch (Exception e)
                {
                    this.log.Error("Unable to fetch the device, or initialize the client or establish a connection. See the exception details for more information.",
                        () => new { this.deviceId, e });
                }
            }
        }

        private void ValidateSetup()
        {
            if (!this.setupDone)
            {
                this.log.Error("Application error: Setup() must be invoked before Run().",
                    () => new { this.deviceId, this.protocol });
                throw new DeviceActorAlreadyInitializedException();
            }
        }
    }
}
