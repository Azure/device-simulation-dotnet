// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    public interface ISimulationContext
    {
        IRateLimiting RateLimiting { get; }
        IDevices Devices { get; }
        ConnectionLoopSettings ConnectionLoopSettings { get; }
        PropertiesLoopSettings PropertiesLoopSettings { get; }

        Task InitAsync(Simulation simulation);

        // Invoked by SimulationManager.NewConnectionLoop()
        //   DeviceConnectionTask.RunAsync()
        //      -> SimulationManager.NewConnectionLoop()
        //         -> ISimulationContext.NewConnectionLoop()
        //void NewConnectionLoop();

        // Invoked by SimulationManager.NewPropertiesLoop()
        //   UpdatePropertiesTask.RunAsync()
        //      -> SimulationManager.NewPropertiesLoop()
        //         -> ISimulationContext.NewPropertiesLoop()
        //void NewPropertiesLoop();
    }

    /// <summary>
    /// Contains all the dependencies for a simulation, to ensure different simulations
    /// don't affect each other, for instance which hub is used, the rating limits, etc. 
    /// </summary>
    public class SimulationContext : ISimulationContext
    {
        public IRateLimiting RateLimiting { get; }
        public IDevices Devices { get; }
        public ConnectionLoopSettings ConnectionLoopSettings { get; private set; }
        public PropertiesLoopSettings PropertiesLoopSettings { get; private set; }

        private readonly IFactory factory;
        private readonly IInstance instance;

        public SimulationContext(
            IDevices devices,
            IRateLimiting rateLimiting,
            IFactory factory,
            IInstance instance)
        {
            this.factory = factory;
            this.instance = instance;
            this.Devices = devices;
            this.RateLimiting = rateLimiting;
        }

        public async Task InitAsync(Simulation simulation)
        {
            this.instance.InitOnce();
            
            // TODO: init using the simulation settings, not the defaults
            var defaultRatingConfig = this.factory.Resolve<IRateLimitingConfig>();
            this.RateLimiting.Init(defaultRatingConfig);
            this.ConnectionLoopSettings = new ConnectionLoopSettings(defaultRatingConfig);
            this.PropertiesLoopSettings = new PropertiesLoopSettings(defaultRatingConfig);

            await this.Devices.InitAsync();
            
            this.instance.InitComplete();
        }

        // TODO: fix usage, resetting the counter this way looks like a bug
        // Invoked by SimulationManager.NewConnectionLoop()
        //   DeviceConnectionTask.RunAsync()
        //      -> SimulationManager.NewConnectionLoop()
        //         -> ISimulationContext.NewConnectionLoop()
        // public void NewConnectionLoop()
        // {
        //     this.instance.InitRequired();
        //     this.ConnectionLoopSettings.NewLoop();
        // }

        // TODO: fix usage, resetting the counter this way looks like a bug
        // Invoked by SimulationManager.NewPropertiesLoop()
        //   UpdatePropertiesTask.RunAsync()
        //      -> SimulationManager.NewPropertiesLoop()
        //         -> ISimulationContext.NewPropertiesLoop()
        // public void NewPropertiesLoop()
        // {
        //     this.instance.InitRequired();
        //     this.PropertiesLoopSettings.NewLoop();
        // }
    }
}
