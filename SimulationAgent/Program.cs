// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Runtime;

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
            
            // TODO: re-enable after migration to .NET Core
            //container.Resolve<Simulation.ISimulation>().Run();
        }
    }
}
