// Copyright (c) Microsoft. All rights reserved.

using Autofac;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    /// <summary>Application entry point</summary>
    public class Program
    {
        static void Main(string[] args)
        {
            var container = DependencyResolution.Setup();
            var logger = container.Resolve<ILogger>();

            logger.Info("Simulation agent started", () => new { Uptime.ProcessId });

            // TODO: re-enable after migration to .NET Core
            container.Resolve<ISimulation>().Run();
        }
    }
}
