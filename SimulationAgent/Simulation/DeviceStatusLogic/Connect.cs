// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
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
        private static readonly TimeSpan retryFrequency = TimeSpan.FromSeconds(10);

        // Device client connection timeout
        private static readonly TimeSpan connectionTimeout = TimeSpan.FromSeconds(5);

        // Device registry fetch/creation timeout
        private static readonly TimeSpan getDeviceTimeout = TimeSpan.FromSeconds(5);

        private readonly IDevices devices;
        private readonly ILogger log;
        private string deviceId;
        private IoTHubProtocol? protocol;

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
            this.timer.Setup(this.Run, retryFrequency);
        }

        public void Start()
        {
            this.timer.Start();
        }

        public void Stop()
        {
            this.timer.Stop();
        }

        public void Run(object context)
        {
            try
            {
                this.log.Info("Starting Connect timer",
                    () => new { this.context.DeviceId });
                this.timer.Pause();
                this.RunInternal();
            }
            finally
            {
                this.log.Info("Stopping Connect timer",
                    () => new { this.context.DeviceId });
                this.timer.Resume();
            }
        }

        private void RunInternal()
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
                    lock (actor)
                    {
                        var device = this.GetDevice(this.deviceId, actor.CancellationToken);

                        actor.Client = this.devices.GetClient(device, this.protocol.Value, this.scriptInterpreter);
                        actor.Client.ConnectAsync().Wait(connectionTimeout);

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
                            actor.BootstrapClient.ConnectAsync().Wait(connectionTimeout);
                        }

                        this.log.Debug("Connection successful", () => new { this.deviceId });

                        actor.MoveNext();
                    }
                }
                catch (Exception e)
                {
                    this.log.Error("Connection failed", () => new { this.deviceId, e });
                }
            }
        }

        private Device GetDevice(string id, CancellationToken cancellationToken)
        {
            this.log.Debug("Connect.Run calling this.devices.GetOrCreateAsync",
                () => new { id, getDeviceTimeout.TotalMilliseconds });

            var task = this.devices.GetOrCreateAsync(this.deviceId, false);
            task.Wait((int) connectionTimeout.TotalMilliseconds, cancellationToken);

            this.log.Debug("Device credentials retrieved", () => new { this.deviceId });

            return task.Result;
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
