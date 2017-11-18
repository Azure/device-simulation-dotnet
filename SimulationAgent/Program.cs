// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Autofac;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    // Application entry point
    public class Program
    {
        static void Main(string[] args)
        {
            // Temporary workaround to allow twin JSON deserialization
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                CheckAdditionalContent = false
            };

            var container = DependencyResolution.Setup();

            // Print some useful information
            PrintBootstrapInfo(container);

            // Increase the available resources
            SetupThreadPool(container);

            // TODO: use async/await with C# 7.1
            container.Resolve<ISimulation>().RunAsync().Wait();
        }

        private static void SetupThreadPool(IContainer container)
        {
            var logger = container.Resolve<ILogger>();

            ThreadPool.GetMaxThreads(out int workerThreads, out int completionPortThreads);
            logger.Info("Original Worker threads: " + workerThreads, () => { });
            logger.Info("Original Asynchronous I/O threads: " + completionPortThreads, () => { });

            workerThreads = Math.Max(workerThreads, 5000);
            completionPortThreads = Math.Max(completionPortThreads, 5000);
            ThreadPool.SetMaxThreads(workerThreads, completionPortThreads);

            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
            logger.Info("New Worker threads: " + workerThreads, () => { });
            logger.Info("New Asynchronous I/O threads: " + completionPortThreads, () => { });
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
            logger.Info("Messages per second:  " + config.ServicesConfig.RateLimiting.DeviceMessagesPerSecond, () => { });
            logger.Info("Messages per day:     " + config.ServicesConfig.RateLimiting.DeviceMessagesPerDay, () => { });
        }
    }
}
