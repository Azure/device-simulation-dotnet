// Copyright (c) Microsoft. All rights reserved.

/*
using System;
using System.Linq;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation
{
    public interface ISimulation
    {
        void Run();
    }

    public class Simulation : ISimulation
    {
        private const int CheckInterval = 3000;

        private readonly ISimulations simulations;
        private readonly ISimulationRunner runner;

        public Simulation(
            ISimulations simulations,
            ISimulationRunner runner)
        {
            this.simulations = simulations;
            this.runner = runner;
        }

        public void Run()
        {
            Console.WriteLine("Simulation Agent running");

            while (true)
            {
                var simulation = this.simulations.GetList().FirstOrDefault();
                if (simulation != null && simulation.Enabled)
                {
                    this.runner.Start(simulation);
                }
                else
                {
                    this.runner.Stop();
                }

                Thread.Sleep(CheckInterval);
            }
        }
    }
}
*/
