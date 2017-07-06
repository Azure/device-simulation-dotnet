// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

// TODO: resilience to IoT Hub manager failures
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
        private readonly IDeviceTypes deviceTypes;
        private readonly DependencyResolution.IFactory factory;
        private readonly List<bool> running;
        private List<IDeviceActor> actors;

        public SimulationRunner(
            ILogger logger,
            IDeviceTypes deviceTypes,
            DependencyResolution.IFactory factory)
        {
            this.log = logger;
            this.deviceTypes = deviceTypes;
            this.factory = factory;

            this.running = new List<bool> { false };
            this.actors = new List<IDeviceActor>();
        }

        public void Start(Services.Models.Simulation simulation)
        {
            lock (this.running)
            {
                // Nothing to do if already running
                if (this.running.FirstOrDefault()) return;

                this.log.Info("Starting simulation", () => new { simulation.Id });
                this.running[0] = true;

                foreach (var dt in simulation.DeviceTypes)
                {
                    var deviceType = this.deviceTypes.Get(dt.Id);

                    for (int i = 0; i < dt.Count; i++)
                    {
                        foreach (DeviceType.DeviceTypeMessage message in deviceType.Telemetry.Messages)
                        {
                            var actor = this.factory.Resolve<IDeviceActor>();

                            try
                            {
                                WaitFor(actor.Setup(deviceType, i, message).ConnectAsync());
                                actor.Start();
                                this.actors.Add(actor);
                            }
                            catch (ExternalDependencyException e)
                            {
                                this.log.Error($"Cannot start actor {i} for {deviceType.Name}", () => new { e.Message });
                            }
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            lock (this.running)
            {
                // Nothing to do if not running
                if (!this.running.FirstOrDefault()) return;

                this.log.Info("Stopping simulation", () => { });

                foreach (var actor in this.actors)
                {
                    actor.Stop();
                }

                this.actors = new List<IDeviceActor>();
                this.running[0] = false;
            }
        }

        /// <summary>
        /// A simple helper to wait for a task and expose the exception,
        /// similarly to what await does
        /// </summary>
        private static void WaitFor(Task task)
        {
            try
            {
                task.Wait();
            }
            catch (AggregateException e)
            {
                throw e.InnerExceptions.First();
            }
        }
    }
}
