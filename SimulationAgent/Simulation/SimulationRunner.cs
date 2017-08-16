// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation
{
    public interface ISimulationRunner
    {
        void Start(Services.Models.Simulation simulation);
        void Stop();
    }

    public class SimulationRunner : ISimulationRunner
    {
        private readonly ILogger log;
        private readonly IDeviceModels deviceModels;
        private readonly DependencyResolution.IFactory factory;
        private readonly List<bool> running;
        private CancellationTokenSource cancellationToken;

        public SimulationRunner(
            ILogger logger,
            IDeviceModels deviceModels,
            DependencyResolution.IFactory factory)
        {
            this.log = logger;
            this.deviceModels = deviceModels;
            this.factory = factory;

            this.running = new List<bool> { false };
        }

        /// <summary>
        /// For each device model in the simulation, create a 'Count'
        /// number of actors, which individually connects and starts
        /// sending messages.
        /// </summary>
        public void Start(Services.Models.Simulation simulation)
        {
            lock (this.running)
            {
                // Nothing to do if already running
                if (this.running.FirstOrDefault()) return;

                this.log.Info("Starting simulation...", () => new { simulation.Id });
                this.cancellationToken = new CancellationTokenSource();

                foreach (var dt in simulation.DeviceModels)
                {
                    var deviceModel = this.deviceModels.Get(dt.Id);
                    Parallel.For(0, dt.Count, i =>
                    {
                        this.log.Debug("Starting device...",
                            () => new { ModelName = deviceModel.Name, ModelId = dt.Id, i });
                        this.factory.Resolve<IDeviceActor>()
                            .Setup(deviceModel, i)
                            .Start(this.cancellationToken.Token);
                    });
                }

                this.running[0] = true;
            }
        }

        public void Stop()
        {
            lock (this.running)
            {
                // Nothing to do if not running
                if (!this.running.FirstOrDefault()) return;

                this.log.Info("Stopping simulation...", () => { });
                this.cancellationToken.Cancel();
                this.running[0] = false;
            }
        }
    }
}
