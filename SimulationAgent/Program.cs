// Copyright (c) Microsoft. All rights reserved.

using System;
using Autofac;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    /// <summary>Application entry point</summary>
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Simulation agent started, process ID: " + Uptime.ProcessId);
            Console.WriteLine($"[{Uptime.ProcessId}] Press [CTRL+C] to quit...");

            var container = DependencyResolution.Setup();
            container.Resolve<ISimulation>().Run();
        }
    }
}
