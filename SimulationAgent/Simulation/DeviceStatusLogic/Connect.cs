// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    /// <summary>
    /// Logic executed after Start(), to establish a connection to IoT Hub.
    /// If the connection fails, the actor retries automatically after some
    /// seconds.
    /// </summary>
    public class Connect : IDeviceStatusLogic
    {
        // When connecting or sending a message, timeout after 5 seconds
        private static readonly TimeSpan connectionTimeout = TimeSpan.FromSeconds(5);

        private readonly IDevices devices;
        private readonly ILogger log;
        private string deviceId;
        private IoTHubProtocol? protocol;
        private readonly IScriptInterpreter scriptInterpreter;

        // Ensure that setup is called once and only once (which helps also detecting thread safety issues)
        private bool setupDone = false;

        public Connect(
            IDevices devices,
            ILogger logger,
            IScriptInterpreter scriptInterpreter)
        {
            this.log = logger;
            this.devices = devices;
            this.scriptInterpreter = scriptInterpreter;
        }

        public void Setup(string deviceId, DeviceModel deviceModel)
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
        }

        public void Run(object context)
        {
            this.ValidateSetup();

            var actor = (IDeviceActor) context;
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
                        this.log.Debug("Connect.Run calling this.devices.GetOrCreateAsync", () => new { this.deviceId, connectionTimeout.TotalMilliseconds });

                        var task = this.devices.GetOrCreateAsync(this.deviceId);
                        task.Wait((int) connectionTimeout.TotalMilliseconds, actor.CancellationToken);
                        var device = task.Result;

                        this.log.Debug("Device credentials retrieved", () => new { this.deviceId });

                        actor.Client = this.devices.GetClient(device, this.protocol.Value, this.scriptInterpreter);

                        // Device Twin properties can be set only over MQTT, so we need a dedicated client
                        // for the bootstrap
                        if (actor.Client.Protocol == IoTHubProtocol.MQTT)
                        {
                            actor.BootstrapClient = actor.Client;
                        }
                        else
                        {
                            // bootstrap client is used to call methods and must have a script interpreter associated w/ it.
                            actor.BootstrapClient = this.devices.GetClient(device, IoTHubProtocol.MQTT, this.scriptInterpreter);
                        }

                        this.log.Debug("Connection successful", () => new { this.deviceId });

                        actor.MoveNext();
                    }
                }
                catch (Exception e)
                {
                    this.log.Error("Connection failed",
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
