// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

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
            this.factory.Resolve<IDevices>().UpdateIotHub();

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
                this.factory.Resolve<IDevices>().UpdateIotHub();

                foreach (var dt in simulation.DeviceModels)
                {
                    if (dt.Count < 1) continue;

                    DeviceModel deviceModel = null;
                    try
                    {
                        deviceModel = this.deviceModels.Get(dt.Id);
                    }
                    catch (ResourceNotFoundException)
                    {
                        this.log.Error("The device model doesn't exist", () => new { dt.Id });
                    }

                    if (deviceModel != null)
                    {
                        Parallel.For(0, dt.Count, i =>
                        {
                            this.log.Debug("Starting device...",
                                () => new { ModelName = deviceModel.Name, ModelId = dt.Id, i });

                            // Note: instances of IDeviceActor are linked to a specific IoT Hub connection string
                            //       so it's important to recreate them when the connection string changes
                            this.factory.Resolve<IDeviceActor>()
                                .Setup(deviceModel, i)
                                .Start(this.cancellationToken.Token);
                        });
                    }
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
