// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation
{
    public interface IRunner
    {
        void Start(Services.Models.Simulation simulation);
        void Stop();
    }

    public class Runner : IRunner
    {
        private readonly List<bool> running;

        public Runner()
        {
            this.running = new List<bool> { false };
        }

        public void Start(Services.Models.Simulation simulation)
        {
            lock (this.running)
            {
                // Nothing to do is already running
                if (this.running.FirstOrDefault()) return;

                Console.WriteLine($"Starting simulation {simulation.Id}...");
                this.running[0] = true;

                foreach (var dt in simulation.DeviceTypes)
                {
                    Console.WriteLine(dt.Id);
                    Console.WriteLine(dt.Count);
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
            }
        }
    }
}
