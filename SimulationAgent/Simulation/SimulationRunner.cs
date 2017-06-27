// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation
{
    public interface ISimulationRunner
    {
        void Start(Services.Models.Simulation simulation);
        void Stop();
    }

    public class SimulationRunner : ISimulationRunner
    {
        private readonly ILogger log;
        private readonly IDeviceTypes deviceTypes;
        private readonly IDevices devices;
        private readonly DependencyResolution.IFactory factory;
        private readonly List<bool> running;
        private List<IDeviceActor> actors;

        public SimulationRunner(
            ILogger logger,
            IDevices devices,
            IDeviceTypes deviceTypes,
            DependencyResolution.IFactory factory)
        {
            this.log = logger;
            this.devices = devices;
            this.deviceTypes = deviceTypes;
            this.factory = factory;

            this.running = new List<bool> { false };
            this.actors = new List<IDeviceActor>();
        }

        public void Start(Services.Models.Simulation simulation)
        {
            lock (this.running)
            {
                // Nothing to do if already running
                if (this.running.FirstOrDefault()) return;

                this.log.Info("Starting simulation", () => new { simulation.Id });
                this.running[0] = true;

                foreach (var dt in simulation.DeviceTypes)
                {
                    var deviceType = this.deviceTypes.Get(dt.Id);

                    for (int i = 0; i < dt.Count; i++)
                    {
                        foreach (DeviceType.DeviceTypeMessage message in deviceType.Telemetry.Messages)
                        {
                            var actor = this.factory.Resolve<IDeviceActor>();
                            actor.Setup(deviceType, i, message).ConnectAsync().Wait();
                            actor.Start();
                            this.actors.Add(actor);
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            lock (this.running)
            {
                // Nothing to do if not running
                if (!this.running.FirstOrDefault()) return;

                this.log.Info("Stopping simulation", () => { });

                foreach (var actor in this.actors)
                {
                    actor.Stop();
                }

                this.actors = new List<IDeviceActor>();
                this.running[0] = false;
            }
        }
    }
}
