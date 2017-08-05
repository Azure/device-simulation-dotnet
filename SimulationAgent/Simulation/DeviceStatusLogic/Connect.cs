// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

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

        private readonly ILogger log;
        private readonly IDevices devices;
        private string deviceId;
        private IoTHubProtocol? protocol;

        public Connect(
            ILogger logger,
            IDevices devices)
        {
            this.log = logger;
            this.devices = devices;
        }

        public void Setup(string deviceId, DeviceType deviceType)
        {
            this.deviceId = deviceId;
            this.protocol = deviceType.Protocol;
        }

        public void Run(object context)
        {
            this.SetupRequired();

            var actor = (IDeviceActor) context;
            if (actor.CancellationToken.IsCancellationRequested)
            {
                actor.Stop();
                return;
            }

            if (actor.ActorStatus == Status.Connecting)
            {
                this.log.Debug("Connecting...", () => { });

                try
                {
                    lock (actor)
                    {
                        this.log.Info("Connect.Run calling this.devices.GetOrCreateAsync", () => new { this.deviceId });

                        var task = this.devices.GetOrCreateAsync(this.deviceId);
                        task.Wait((int)connectionTimeout.TotalMilliseconds, actor.CancellationToken);
                        var device = task.Result;

                        this.log.Debug("Device credentials retrieved", () => new { this.deviceId });

                        actor.Client = this.devices.GetClient(device, this.protocol.Value);
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

        private void SetupRequired()
        {
            if (this.deviceId == null || this.protocol == null)
            {
                throw new Exception("Application error: Setup() must be invoked before Run().");
            }
        }
    }
}
