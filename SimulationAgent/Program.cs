// Copyright (c) Microsoft. All rights reserved.

using Autofac;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    // Application entry point
    public class Program
    {
        static void Main(string[] args)
        {
            var container = DependencyResolution.Setup();

            // Print some useful information
            PrintBootstrapInfo(container);

            container.Resolve<ISimulation>().RunAsync().Wait();
        }

        private static void PrintBootstrapInfo(IContainer container)
        {
            var logger = container.Resolve<ILogger>();
            var config = container.Resolve<IConfig>();
            logger.Info("Simulation agent started", () => new { Uptime.ProcessId });
            logger.Info("Device Models folder: " + config.ServicesConfig.DeviceModelsFolder, () => { });
            logger.Info("Scripts folder:       " + config.ServicesConfig.DeviceModelsScriptsFolder, () => { });

            logger.Info("Connections per sec:  " + config.ServicesConfig.RateLimiting.ConnectionsPerSecond, () => { });
            logger.Info("Registry ops per sec: " + config.ServicesConfig.RateLimiting.RegistryOperationsPerMinute, () => { });
            logger.Info("Twin reads per sec:   " + config.ServicesConfig.RateLimiting.TwinReadsPerSecond, () => { });
            logger.Info("Twin writes per sec:  " + config.ServicesConfig.RateLimiting.TwinWritesPerSecond, () => { });
            logger.Info("Messages per day:     " + config.ServicesConfig.RateLimiting.MessagesPerDay, () => { });
        }
    }
}
