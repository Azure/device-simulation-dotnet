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

            var rateLimits = simulation.RateLimits;
            this.RateLimiting.Init(rateLimits);
            this.ConnectionLoopSettings = new ConnectionLoopSettings(rateLimits);
            this.PropertiesLoopSettings = new PropertiesLoopSettings(rateLimits);

            await this.Devices.InitAsync();

            this.instance.InitComplete();
        }
    }
}
