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
            Console.WriteLine("Starting simulation agent");
            Console.WriteLine("Process ID:" + Uptime.ProcessId);

            var container = DependencyInjection.GetContainer();
            container.Resolve<ISimulation>().Run();
        }
    }
}
