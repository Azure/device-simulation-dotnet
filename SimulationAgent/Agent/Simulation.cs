// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Agent
{
    public interface ISimulation
    {
        void Run();
    }

    public class Simulation : ISimulation
    {
        private const int CheckInterval = 3000;

        private readonly ISimulations simulations;
        private readonly List<bool> running;

        public Simulation(ISimulations simulations)
        {
            this.simulations = simulations;
            this.running = new List<bool> { false };
        }

        public void Run()
        {
            Console.WriteLine("Simulation Agent running");

            while (true)
            {
                var simulation = this.simulations.GetList().FirstOrDefault();
                if (simulation != null && simulation.Enabled)
                {
                    this.StartDevices(simulation);
                }
                else
                {
                    this.StopDevices();
                }

                Thread.Sleep(CheckInterval);
            }
        }

        private void StartDevices(Services.Models.Simulation simulation)
        {
            lock (this.running)
            {
                if (this.running.FirstOrDefault()) return;

                Console.WriteLine($"Starting simulation {simulation.Id}...");
                this.running[0] = true;
            }
        }

        private void StopDevices()
        {
            lock (this.running)
            {
                if (!this.running.FirstOrDefault()) return;

                Console.WriteLine("Stopping simulation...");
                this.running[0] = false;
            }
        }
    }
}
