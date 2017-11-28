// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    public interface ISimulationRunner
    {
        void Start(Services.Models.Simulation simulation);
        void Stop();
    }

    public class SimulationRunner : ISimulationRunner
    {
        // ID prefix of the simulated devices, used with Azure IoT Hub
        private const string DEVICE_ID_PREFIX = "Simulated.";

        private const int MIN_STATE_LOOP_DURATION = 1000;
        private const int MIN_TELEMETRY_LOOP_DURATION = 500;

        // Application logger
        private readonly ILogger log;

        // Service used to load device models details
        private readonly IDeviceModels deviceModels;

        // DI factory used to instantiate actors
        private readonly IFactory factory;

        // List of all the actors managing the devices state
        private readonly IDictionary<string, IDeviceStateActor> deviceStateActors;

        // Contains all the actors responsible to connect the devices
        private readonly IDictionary<string, IDeviceConnectionActor> deviceConnectionActors;

        // Contains all the actors sending telemetry
        private readonly IDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors;

        // The thread responsible for updating devices/sensors state
        private Thread devicesStateThread;

        // The thread responsible for connecting all the devices to Azure IoT Hub
        private Thread devicesConnectionThread;

        // The thread responsible for sending telemetry to Azure IoT Hub
        private Thread devicesTelemetryThread;

        // Simple lock objects toi avoid contentions
        private readonly object startLock;

        // Flag signaling whether the simulation is starting (to reduce blocked threads)
        private bool starting;

        // Flag signaling whether the simulation has started and is running (to avoid contentions)
        private bool running;

        public SimulationRunner(
            ILogger logger,
            IDeviceModels deviceModels,
            IFactory factory)
        {
            this.log = logger;
            this.deviceModels = deviceModels;
            this.factory = factory;

            this.startLock = new { };
            this.running = false;
            this.starting = false;

            this.deviceStateActors = new ConcurrentDictionary<string, IDeviceStateActor>();
            this.deviceConnectionActors = new ConcurrentDictionary<string, IDeviceConnectionActor>();
            this.deviceTelemetryActors = new ConcurrentDictionary<string, IDeviceTelemetryActor>();
        }

        /// <summary>
        /// For each device model in the simulation, create a 'Count'
        /// number of actors, each responsible for updating the simulated device state.
        /// 
        /// For each device model in the simulation, and for each message in the model,
        /// create an actor responsible for
        /// sending the telemetry.
        /// </summary>
        public void Start(Services.Models.Simulation simulation)
        {
            // Use `starting` to exit as soon as possible, to minimize the number 
            // of threads pending on the lock statement
            if (this.starting || this.running) return;

            lock (this.startLock)
            {
                if (this.running) return;

                // Use `starting` to exit as soon as possible, to minimize the number 
                // of threads pending on the lock statement
                this.starting = true;

                this.log.Info("Starting simulation...", () => new { simulation.Id });

                // Loop through all the device models used in the simulation
                var models = from model in simulation.DeviceModels where model.Count > 0 select model;

                // Calculate the total number of devices
                var total = models.Sum(model => model.Count);

                foreach (var model in models)
                {
                    // Load device model from disk
                    DeviceModel deviceModel;
                    try
                    {
                        deviceModel = this.deviceModels.Get(model.Id);
                    }
                    catch (ResourceNotFoundException)
                    {
                        this.log.Error("The device model doesn't exist", () => new { model.Id });
                        continue;
                    }

                    for (var i = 0; i < model.Count; i++)
                    {
                        this.CreateActorsForDevice(deviceModel, model, i, total);
                    }
                }

                // Use `running` to avoid starting the simulation more than once
                this.running = true;

                // Reset, just in case
                this.starting = false;

                // Start threads
                this.devicesStateThread = new Thread(this.UpdateDevicesStateThread);
                this.devicesStateThread.Start();

                this.devicesConnectionThread = new Thread(this.ConnectDevicesThread);
                this.devicesConnectionThread.Start();

                this.devicesTelemetryThread = new Thread(this.SendTelemetryThread);
                this.devicesTelemetryThread.Start();
            }
        }

        public void Stop()
        {
            lock (this.startLock)
            {
                if (!this.running) return;

                this.log.Info("Stopping simulation...", () => { });

                this.running = false;

                foreach (var device in this.deviceConnectionActors)
                {
                    device.Value.Stop();
                }

                foreach (var device in this.deviceTelemetryActors)
                {
                    device.Value.Stop();
                }

                // Allow 3 seconds to complete before stopping the threads
                Thread.Sleep(3000);
                this.TryToStopStateThread();
                this.TryToStopConnectionThread();
                this.TryToStopTelemetryThread();

                // Reset local state
                this.deviceStateActors.Clear();
                this.deviceTelemetryActors.Clear();
                this.deviceConnectionActors.Clear();
                this.starting = false;
            }
        }

        private void UpdateDevicesStateThread()
        {
            while (this.running)
            {
                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (var device in this.deviceStateActors)
                {
                    device.Value.Run();
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Info("Device state loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, MIN_STATE_LOOP_DURATION);
            }
        }

        private void ConnectDevicesThread()
        {
            while (this.running)
            {
                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (var device in this.deviceConnectionActors)
                {
                    device.Value.Run();
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Info("Device state loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, MIN_STATE_LOOP_DURATION);
            }
        }

        private void SendTelemetryThread()
        {
            while (this.running)
            {
                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (var telemetry in this.deviceTelemetryActors)
                {
                    telemetry.Value.Run();
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Info("Telemetry loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, MIN_TELEMETRY_LOOP_DURATION);
            }
        }

        private void SlowDownIfTooFast(long duration, int min)
        {
            // Avoid 1msec sleeps
            if (duration >= min || min - duration <= 1) return;

            Thread.Sleep(min - (int) duration);
        }

        /**
         * For each device create one actor to periodically update the internal state,
         * one actor to manage the connection to the hub, and one actor for each
         * telemetry message to send.
         */
        private void CreateActorsForDevice(DeviceModel deviceModel, Services.Models.Simulation.DeviceModelRef dm, int position, int total)
        {
            var deviceId = DEVICE_ID_PREFIX + deviceModel.Id + "." + position;
            var key = deviceModel.Id + "#" + position;

            this.log.Debug("Creating device actors...",
                () => new { ModelName = deviceModel.Name, ModelId = dm.Id, position });

            // Create one state actor for each device
            var deviceStateActor = this.factory.Resolve<IDeviceStateActor>();
            deviceStateActor.Setup(deviceId, deviceModel, position, total);
            this.deviceStateActors.Add(key, deviceStateActor);

            // Create one connection actor for each device
            var deviceConnectionActor = this.factory.Resolve<IDeviceConnectionActor>();
            deviceConnectionActor.Setup(deviceId, deviceModel, deviceStateActor);
            this.deviceConnectionActors.Add(key, deviceConnectionActor);

            // Create one telemetry actor for each telemetry message to be sent
            foreach (var message in deviceModel.Telemetry)
            {
                var deviceTelemetryActor = this.factory.Resolve<IDeviceTelemetryActor>();
                deviceTelemetryActor.Setup(deviceId, deviceModel, message, deviceStateActor, deviceConnectionActor);
                this.deviceTelemetryActors.Add(key, deviceTelemetryActor);
            }
        }

        private void TryToStopTelemetryThread()
        {
            try
            {
                this.devicesTelemetryThread.Interrupt();
            }
            catch (Exception e)
            {
                this.log.Warn("Unable to stop the telemetry thread in a clean way", () => new { e });
            }
        }

        private void TryToStopConnectionThread()
        {
            try
            {
                this.devicesConnectionThread.Interrupt();
            }
            catch (Exception e)
            {
                this.log.Warn("Unable to stop the connections thread in a clean way", () => new { e });
            }
        }

        private void TryToStopStateThread()
        {
            try
            {
                this.devicesStateThread.Interrupt();
            }
            catch (Exception e)
            {
                this.log.Warn("Unable to stop the devices state thread in a clean way", () => new { e });
            }
        }
    }
}
