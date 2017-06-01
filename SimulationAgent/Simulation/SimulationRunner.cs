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
        private List<IActorRef> actors;

        public SimulationRunner(
            IDeviceTypes deviceTypes,
            IActorRefFactory actorSystem)
        {
            this.deviceTypes = deviceTypes;
            this.actorSystem = actorSystem;

            this.running = new List<bool> { false };
            this.actors = new List<IActorRef>();
        }

        /// <summary>
        /// For each device type create a set of indipendent actors.
        /// Each actor takes care of simulating a device.
        /// </summary>
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

                        actor.Tell(new DeviceActorMessages.Setup(i, deviceType));
                        actor.Tell(new DeviceActorMessages.Start());
                    }
                }
            }
        }

        /// <summary>
        /// Send a message to all the actors asking to stop running
        /// </summary>
        public void Stop()
        {
            lock (this.running)
            {
                // Nothing to do if not running
                if (!this.running.FirstOrDefault()) return;

                Console.WriteLine("Stopping simulation...");

                // TODO: use Akka pubsub
                /* This approach to stop the actors works fine and keeps
                the logs clean, however it doesnt scale when there are
                thousands/millions of actors. TO DO: (1) organize the actors
                under few master nodes, (2) use pubsub to tell the cluster to
                stop, and (3) kill the master nodes after few seconds. */
                foreach (var actor in this.actors)
                {
                    actor.Tell(new DeviceActorMessages.Stop());
                    actor.GracefulStop(TimeSpan.FromSeconds(10));
                }

                this.actors = new List<IActorRef>();
                this.running[0] = false;
            }
        }
    }
}
