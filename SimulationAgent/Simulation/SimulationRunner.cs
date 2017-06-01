// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
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
        private readonly IDeviceTypes deviceTypes;
        private readonly IActorRefFactory actorSystem;
        private readonly List<bool> running;
        private readonly List<IActorRef> actors;

        public SimulationRunner(
            IDeviceTypes deviceTypes,
            IActorRefFactory actorSystem)
        {
            this.deviceTypes = deviceTypes;
            this.actorSystem = actorSystem;

            this.running = new List<bool> { false };
            this.actors = new List<IActorRef>();
        }

        public void Start(Services.Models.Simulation simulation)
        {
            lock (this.running)
            {
                // Nothing to do if already running
                if (this.running.FirstOrDefault()) return;

                Console.WriteLine($"Starting simulation {simulation.Id}...");
                this.running[0] = true;

                foreach (var dt in simulation.DeviceTypes)
                {
                    DeviceType deviceType = this.deviceTypes.Get(dt.Id);

                    for (int i = 0; i < dt.Count; i++)
                    {
                        var actor = this.actorSystem.ActorOf<DeviceActor>();
                        this.actors.Add(actor);

                        actor.Tell(new DeviceActor.Setup(i, deviceType));
                        actor.Tell(new DeviceActor.Start());
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

                Console.WriteLine("Stopping simulation...");
                this.running[0] = false;

                foreach (var actor in this.actors)
                {
                    actor.Tell(new DeviceActor.Stop());
                }
            }
        }
    }
}
