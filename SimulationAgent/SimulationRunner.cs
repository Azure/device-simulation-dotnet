// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    public interface ISimulationRunner
    {
        void Start(Services.Models.Simulation simulation);
        void Stop();
        long ActiveDevicesCount { get; }
        long TotalMessagesCount { get; }
        long FailedMessagesCount { get; }
        long FailedDeviceConnectionsCount { get; }
        long FailedDeviceTwinUpdatesCount { get; }
        long SimulationErrorsCount { get; }
    }

    public class SimulationRunner : ISimulationRunner
    {
        // Allow 30 seconds to create the devices (1000 devices normally takes 2-3 seconds)
        private const int DEVICES_CREATION_TIMEOUT_SECS = 30;

        // Application logger
        private readonly ILogger log;

        // Settings to optimize scheduling
        private readonly ConnectionLoopSettings connectionLoopSettings;

        // Settings to optimize scheduling
        private readonly PropertiesLoopSettings propertiesLoopSettings;

        // Service used to load device models details
        private readonly IDeviceModels deviceModels;

        // Logic used to prepare device models, overriding settings as requested by the UI
        private readonly IDeviceModelsGeneration deviceModelsOverriding;

        // Reference to the singleton used to access the devices
        private readonly IDevices devices;

        // Service used to manage simulation details
        private readonly ISimulations simulations;

        // DI factory used to instantiate actors
        private readonly IFactory factory;

        // List of all the actors managing the devices state
        private readonly IDictionary<string, IDeviceStateActor> deviceStateActors;

        // Contains all the actors responsible to connect the devices
        private readonly IDictionary<string, IDeviceConnectionActor> deviceConnectionActors;

        // Contains all the actors sending telemetry
        private readonly IDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors;

        // Contains all the actors sending device property updates to Azure IoT Hub
        private readonly IDictionary<string, IDevicePropertiesActor> devicePropertiesActors;

        // Service used to reset all rateLimiting counters
        private readonly IRateLimiting rateLimiting;

        // The thread responsible for updating devices/sensors state
        private Thread devicesStateThread;

        // The thread responsible for connecting all the devices to Azure IoT Hub
        private Thread devicesConnectionThread;

        // The thread responsible for sending telemetry to Azure IoT Hub
        private Thread devicesTelemetryThread;

        // The thread responsible for sending device property updates to Azure IoT Hub
        private Thread devicesPropertiesThread;

        // Simple lock objects toi avoid contentions
        private readonly object startLock;

        // Flag signaling whether the simulation is starting (to reduce blocked threads)
        private bool starting;

        // Flag signaling whether the simulation has started and is running (to avoid contentions)
        private bool running;

        // Counter for simulation error
        private long simulationErrors;

        public SimulationRunner(
            IRateLimitingConfig ratingConfig,
            IRateLimiting rateLimiting,
            ILogger logger,
            IDeviceModels deviceModels,
            IDeviceModelsGeneration deviceModelsOverriding,
            IDevices devices,
            ISimulations simulations,
            IFactory factory)
        {
            this.connectionLoopSettings = new ConnectionLoopSettings(ratingConfig);
            this.propertiesLoopSettings = new PropertiesLoopSettings(ratingConfig);

            this.log = logger;
            this.deviceModels = deviceModels;
            this.deviceModelsOverriding = deviceModelsOverriding;
            this.devices = devices;
            this.simulations = simulations;
            this.factory = factory;

            this.startLock = new { };
            this.running = false;
            this.starting = false;
            this.rateLimiting = rateLimiting;

            this.deviceStateActors = new ConcurrentDictionary<string, IDeviceStateActor>();
            this.deviceConnectionActors = new ConcurrentDictionary<string, IDeviceConnectionActor>();
            this.deviceTelemetryActors = new ConcurrentDictionary<string, IDeviceTelemetryActor>();
            this.devicePropertiesActors = new ConcurrentDictionary<string, IDevicePropertiesActor>();
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

                // Note: this is a singleton class, so we can call this once. This sets
                // the active hub, e.g. in case the user provided a custom connection string.
                this.devices.SetCurrentIotHub();

                // Create the devices
                try
                {
                    var devices = this.simulations.GetDeviceIds(simulation);

                    // This will ignore existing devices, creating only the missing ones
                    this.devices.CreateListAsync(devices)
                        .Wait(TimeSpan.FromSeconds(DEVICES_CREATION_TIMEOUT_SECS));
                }
                catch (Exception e)
                {
                    this.running = false;
                    this.starting = false;
                    this.log.Error("Failed to create devices", () => new { e });
                    this.IncreamentSimulationErrorsCount();

                    // Return and retry
                    return;
                }

                // Loop through all the device models used in the simulation
                var models = (from model in simulation.DeviceModels where model.Count > 0 select model).ToList();

                // Calculate the total number of devices
                var total = models.Sum(model => model.Count);

                foreach (var model in models)
                {
                    try
                    {
                        // Load device model from disk and merge with overrides
                        var deviceModel = this.GetDeviceModel(model.Id, model.Override);

                        for (var i = 0; i < model.Count; i++)
                        {
                            this.CreateActorsForDevice(deviceModel, i, total);
                        }
                    }
                    catch (ResourceNotFoundException)
                    {
                        this.IncreamentSimulationErrorsCount();
                        this.log.Error("The device model doesn't exist", () => new { model.Id });
                    }
                    catch (Exception e)
                    {
                        this.IncreamentSimulationErrorsCount();
                        this.log.Error("Unexpected error preparing the device model", () => new { model.Id, e });
                    }
                }

                // Use `running` to avoid starting the simulation more than once
                this.running = true;

                // Reset, just in case
                this.starting = false;

                // Start threads
                this.TryToStartStateThread();

                this.TryToStartConnectionThread();

                this.TryToStartTelemetryThread();

                this.TryToStartPropertiesThread();
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

                foreach (var device in this.devicePropertiesActors)
                {
                    device.Value.Stop();
                }

                // Allow 3 seconds to complete before stopping the threads
                Thread.Sleep(3000);
                this.TryToStopStateThread();
                this.TryToStopConnectionThread();
                this.TryToStopTelemetryThread();
                this.TryToStopPropertiesThread();

                // Reset local state
                this.deviceStateActors.Clear();
                this.deviceTelemetryActors.Clear();
                this.deviceConnectionActors.Clear();
                this.devicePropertiesActors.Clear();
                this.starting = false;

                // Reset rateLimiting counters
                this.rateLimiting.ResetCounters();
            }
        }

        // Method to return the count of active devices
        public long ActiveDevicesCount => this.deviceStateActors.Count(a => a.Value.IsDeviceActive);

        // Method to return the count of total messages
        public long TotalMessagesCount => this.deviceTelemetryActors.Sum(a => a.Value.TotalMessagesCount);

        // Method to return the count of deliver failed messages
        public long FailedMessagesCount => this.deviceTelemetryActors.Sum(a => a.Value.FailedMessagesCount);

        // Method to return the count of connection failed devices
        public long FailedDeviceConnectionsCount => this.deviceConnectionActors.Sum(a => a.Value.FailedDeviceConnectionsCount);

        // Method to return the count of twin update failed devices
        public long FailedDeviceTwinUpdatesCount => this.devicePropertiesActors.Sum(a => a.Value.FailedTwinUpdatesCount);

        // Method to return the count of simulation errors
        public long SimulationErrorsCount => this.simulationErrors +
                this.deviceConnectionActors.Sum(a => a.Value.SimulationErrorsCount) +
                this.deviceStateActors.Sum(a => a.Value.SimulationErrorsCount) +
                this.deviceTelemetryActors.Sum(a => a.Value.FailedMessagesCount) +
                this.devicePropertiesActors.Sum(a => a.Value.SimulationErrorsCount);

        private DeviceModel GetDeviceModel(string id, Services.Models.Simulation.DeviceModelOverride overrides)
        {
            var modelDef = new DeviceModel();
            if (id.ToLowerInvariant() != DeviceModels.CUSTOM_DEVICE_MODEL_ID.ToLowerInvariant())
            {
                modelDef = this.deviceModels.Get(id);
            }
            else
            {
                modelDef.Id = DeviceModels.CUSTOM_DEVICE_MODEL_ID;
                modelDef.Name = DeviceModels.CUSTOM_DEVICE_MODEL_ID;
                modelDef.Description = "Simulated device with custom list of sensors";
            }

            return this.deviceModelsOverriding.Generate(modelDef, overrides);
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
                this.SlowDownIfTooFast(durationMsecs, StateLoopSettings.MIN_LOOP_DURATION);
            }
        }

        private void ConnectDevicesThread()
        {
            while (this.running)
            {
                this.connectionLoopSettings.NewLoop();
                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                foreach (var device in this.deviceConnectionActors)
                {
                    device.Value.Run();
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Info("Device state loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, ConnectionLoopSettings.MIN_LOOP_DURATION);
            }
        }

        private void UpdatePropertiesThread()
        {
            while (this.running)
            {
                this.propertiesLoopSettings.NewLoop();

                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                foreach (var device in this.devicePropertiesActors)
                {
                    device.Value.Run();
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Info("Device properties loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, ConnectionLoopSettings.MIN_LOOP_DURATION);
            }
        }

        private void SendTelemetryThread()
        {
            if (this.deviceTelemetryActors.Count == 0)
            {
                this.log.Warn("There is no telemetry to send, stopping the telemetry thread", () => { });
                return;
            }

            var stats = new Dictionary<string, int>();
            while (this.running)
            {
                // Keep count of what the actors are doing and log it
                if (this.log.InfoIsEnabled)
                {
                    stats.Clear();
                    stats["nothingToDo"] = 0;
                }

                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                foreach (var telemetry in this.deviceTelemetryActors)
                {
                    var stat = telemetry.Value.Run();
                    if (this.log.InfoIsEnabled)
                    {
                        if (stat != null)
                        {
                            stats[stat] = stats.ContainsKey(stat) ? stats[stat] + 1 : 1;
                        }
                        else
                        {
                            stats["nothingToDo"]++;
                        }
                    }
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Info("Telemetry loop completed", () => new { durationMsecs, stats });
                this.SlowDownIfTooFast(durationMsecs, TelemetryLoopSettings.MIN_LOOP_DURATION);
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
        private void CreateActorsForDevice(DeviceModel deviceModel, int position, int total)
        {
            var deviceId = this.devices.GenerateId(deviceModel.Id, position);
            var key = deviceModel.Id + "#" + position;

            this.log.Debug("Creating device actors...",
                () => new { ModelName = deviceModel.Name, ModelId = deviceModel.Id, position });

            // Create one state actor for each device
            var deviceStateActor = this.factory.Resolve<IDeviceStateActor>();
            deviceStateActor.Setup(deviceId, deviceModel, position, total);
            this.deviceStateActors.Add(key, deviceStateActor);

            // Create one connection actor for each device
            var deviceConnectionActor = this.factory.Resolve<IDeviceConnectionActor>();
            deviceConnectionActor.Setup(deviceId, deviceModel, deviceStateActor, this.connectionLoopSettings);
            this.deviceConnectionActors.Add(key, deviceConnectionActor);

            // Create one device properties actor for each device
            var devicePropertiesActor = this.factory.Resolve<IDevicePropertiesActor>();
            devicePropertiesActor.Setup(deviceId, deviceStateActor, deviceConnectionActor, this.propertiesLoopSettings);
            this.devicePropertiesActors.Add(key, devicePropertiesActor);

            // Create one telemetry actor for each telemetry message to be sent
            var i = 0;
            foreach (var message in deviceModel.Telemetry)
            {
                // Skip telemetry without an interval set
                if (!(message.Interval.TotalMilliseconds > 0))
                {
                    this.log.Warn("Skipping telemetry with interval = 0",
                        () => new { model = deviceModel.Id, message });
                    continue;
                }

                var deviceTelemetryActor = this.factory.Resolve<IDeviceTelemetryActor>();
                deviceTelemetryActor.Setup(deviceId, deviceModel, message, deviceStateActor, deviceConnectionActor);

                var actorKey = key + "#" + (i++).ToString();
                this.deviceTelemetryActors.Add(actorKey, deviceTelemetryActor);
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

        private void TryToStopPropertiesThread()
        {
            try
            {
                this.devicesPropertiesThread.Interrupt();
            }
            catch (Exception e)
            {
                this.log.Warn("Unable to stop the devices state thread in a clean way", () => new { e });
            }
        }

        private void TryToStartTelemetryThread()
        {
            this.devicesTelemetryThread = new Thread(this.SendTelemetryThread);
            try
            {
                this.devicesTelemetryThread.Start();
            }
            catch (Exception e)
            {
                this.IncreamentSimulationErrorsCount();
                this.log.Error("Unable to start the telemetry thread", () => new { e });
                throw new Exception("Unable to start the telemetry thread", e);
            }
        }

        private void TryToStartConnectionThread()
        {
            this.devicesConnectionThread = new Thread(this.ConnectDevicesThread);
            try
            {
                this.devicesConnectionThread.Start();
            }
            catch (Exception e)
            {
                this.IncreamentSimulationErrorsCount();
                this.log.Error("Unable to start the device connection thread", () => new { e });
                throw new Exception("Unable to start the device connection thread", e);
            }
        }

        private void TryToStartStateThread()
        {
            this.devicesStateThread = new Thread(this.UpdateDevicesStateThread);
            try
            {
                this.devicesStateThread.Start();
            }
            catch (Exception e)
            {
                this.IncreamentSimulationErrorsCount();
                this.log.Error("Unable to start the device state thread", () => new { e });
                throw new Exception("Unable to start the device state thread", e);
            }
        }

        private void TryToStartPropertiesThread()
        {
            this.devicesPropertiesThread = new Thread(this.UpdatePropertiesThread);
            try
            {
                this.devicesPropertiesThread.Start();
            }
            catch (Exception e)
            {
                this.IncreamentSimulationErrorsCount();
                this.log.Error("Unable to start the device properties thread", () => new { e });
                throw new Exception("Unable to start the device properties thread", e);
            }
        }

        private void IncreamentSimulationErrorsCount()
        {
            Interlocked.Increment(ref this.simulationErrors);
        }
    }
}
