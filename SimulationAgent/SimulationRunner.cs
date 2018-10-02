// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        Task StartAsync(Simulation simulation);
        void Stop();
        Task AddDeviceAsync(string deviceId, string modelId);
        void DeleteDevices(List<string> ids);
        long ActiveDevicesCount { get; }
        long TotalMessagesCount { get; }
        long FailedMessagesCount { get; }
        long FailedDeviceConnectionsCount { get; }
        long FailedDeviceTwinUpdatesCount { get; }
        long SimulationErrorsCount { get; }
    }

    public class SimulationRunner : ISimulationRunner
    {
        // Allow time to obtain the IoT Hub connection string from storage
        private const int DEVICES_INIT_TIMEOUT_SECS = 5;

        // Allow 30 seconds to create the devices (1000 devices normally takes 2-3 seconds)
        private const int DEVICES_CREATION_TIMEOUT_SECS = 30;

        // Application logger
        private readonly ILogger log;

        // Diagnostics logger
        private readonly IDiagnosticsLogger diagnosticsLogger;

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

        // Configure concurrency, threads, etc.
        private readonly ISimulationConcurrencyConfig simulationConcurrencyConfig;

        // The thread responsible for updating devices/sensors state
        private Thread devicesStateThread;

        // The thread responsible for connecting all the devices to Azure IoT Hub
        private Thread devicesConnectionThread;

        // Array of threads used to send telemetry
        private Thread[] devicesTelemetryThreads;

        // The thread responsible for sending device property updates to Azure IoT Hub
        private Thread devicesPropertiesThread;

        // Simple lock objects toi avoid contentions
        private readonly object startLock;

        // Flags signaling whether the simulation is starting or stopping (to reduce blocked threads)
        private bool starting;
        private bool stopping;

        // Flag signaling whether the simulation has started and is running (to avoid contentions)
        private bool running;

        // Counter for simulation error
        private long simulationErrors;

        public SimulationRunner(
            IRateLimitingConfig ratingConfig,
            IRateLimiting rateLimiting,
            ISimulationConcurrencyConfig simulationConcurrencyConfig,
            ILogger logger,
            IDiagnosticsLogger diagnosticsLogger,
            IDeviceModels deviceModels,
            IDeviceModelsGeneration deviceModelsOverriding,
            IDevices devices,
            IFactory factory)
        {
            this.connectionLoopSettings = new ConnectionLoopSettings(ratingConfig);
            this.propertiesLoopSettings = new PropertiesLoopSettings(ratingConfig);

            this.simulationConcurrencyConfig = simulationConcurrencyConfig;
            this.log = logger;
            this.diagnosticsLogger = diagnosticsLogger;
            this.deviceModels = deviceModels;
            this.deviceModelsOverriding = deviceModelsOverriding;
            this.devices = devices;
            this.factory = factory;

            this.startLock = new { };
            this.running = false;
            this.starting = false;
            this.stopping = false;
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
        public async Task StartAsync(Simulation simulation)
        {
            // Use `starting` to exit as soon as possible, to minimize the number 
            // of threads pending on the lock statement
            if (this.starting || this.stopping || this.running) return;

            // Use `starting` to exit as soon as possible, to minimize the number 
            // of threads pending on the lock statement
            this.starting = true;

            this.log.Info("Starting simulation...", () => new { simulation.Id });

            // TODO: to be removed once SimulationContext is introduced
            await this.devices.InitAsync();

            // Loop through all the device models used in the simulation
            var models = (from model in simulation.DeviceModels where model.Count > 0 select model).ToList();

            // Calculate the total number of devices
            // the active hub, e.g. in case the user provided a custom connection string.
            var total = models.Sum(model => model.Count);

            // Loop through all the device models used in the simulation
            foreach (var model in models)
            {
                try
                {
                    // Load device model and merge with overrides
                    var deviceModel = this.GetDeviceModel(model.Id, model.Override);

                    for (var i = 0; i < model.Count; i++)
                    {
                        await this.CreateActorsForDeviceAsync(deviceModel, i, total);
                    }
                }
                catch (ResourceNotFoundException)
                {
                    var msg = "The device model doesn't exist";
                    this.IncrementSimulationErrorsCount();
                    this.log.Error(msg, () => new { model.Id });
                    this.diagnosticsLogger.LogServiceError(msg, new { model.Id });
                }
                catch (Exception e)
                {
                    var msg = "Unexpected error preparing the device model";
                    this.IncrementSimulationErrorsCount();
                    this.log.Error(msg, () => new { model.Id, e });
                    this.diagnosticsLogger.LogServiceError(msg, new { model.Id, e.Message });
                }
            }

            foreach (var customDevice in simulation.CustomDevices)
            {
                await this.AddCustomDeviceAsync(customDevice.DeviceId, customDevice.DeviceModel.Id);
            }

            // Use `running` to avoid starting the simulation more than once
            this.running = true;

            // Reset, just in case
            this.starting = false;

            // Start threads
            this.TryToStartStateThread();

            this.TryToStartConnectionThread();

            this.TryToStartTelemetryThreads();

            this.TryToStartPropertiesThread();
        }

        public void Stop()
        {
            lock (this.startLock)
            {
                if (!this.running) return;

                this.log.Info("Stopping simulation...");

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
                this.TryToStopTelemetryThreads();
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

        public async Task AddDeviceAsync(string deviceId, string modelId)
        {
            await this.AddCustomDeviceAsync(deviceId, modelId);
        }

        /// <summary>
        /// Delete a list of devices
        /// </summary>
        public void DeleteDevices(List<string> ids)
        {
            foreach (var device in this.deviceConnectionActors)
            {
                var deviceId = device.Value.Device.Id;

                if (ids.Contains(deviceId))
                {
                    this.log.Info("Deleting device", () => new { deviceId });
                    device.Value.Delete();
                }
            }
        }

        // Method to return the count of active devices
        public long ActiveDevicesCount => this.deviceStateActors.Count(a => a.Value.IsDeviceActive);

        // Method to return the count of total messages
        public long TotalMessagesCount => this.deviceTelemetryActors.Sum(a => a.Value.TotalMessagesCount);

        // Method to return the count of delivery-failed messages
        public long FailedMessagesCount => this.deviceTelemetryActors.Sum(a => a.Value.FailedMessagesCount);

        // Method to return the count of connection-failed devices
        public long FailedDeviceConnectionsCount => this.deviceConnectionActors.Sum(a => a.Value.FailedDeviceConnectionsCount);

        // Method to return the count of twin update-failed devices
        public long FailedDeviceTwinUpdatesCount => this.devicePropertiesActors.Sum(a => a.Value.FailedTwinUpdatesCount);

        // Method to return the count of simulation errors
        public long SimulationErrorsCount => this.simulationErrors +
                                             this.deviceConnectionActors.Sum(a => a.Value.SimulationErrorsCount) +
                                             this.deviceStateActors.Sum(a => a.Value.SimulationErrorsCount) +
                                             this.deviceTelemetryActors.Sum(a => a.Value.FailedMessagesCount) +
                                             this.devicePropertiesActors.Sum(a => a.Value.SimulationErrorsCount);

        private DeviceModel GetDeviceModel(string id, Simulation.DeviceModelOverride overrides)
        {
            var modelDef = new DeviceModel();
            if (id.ToLowerInvariant() != DeviceModels.CUSTOM_DEVICE_MODEL_ID.ToLowerInvariant())
            {
                try
                {
                    var task = this.deviceModels.GetAsync(id);
                    task.Wait(TimeSpan.FromSeconds(30));
                    modelDef = task.Result;
                }
                catch (AggregateException ae)
                {
                    throw new ExternalDependencyException("Unable to load device model", ae.InnerException);
                }
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
                this.log.Debug("Device state loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, this.simulationConcurrencyConfig.MinDeviceStateLoopDuration);
            }
        }

        private void ConnectDevicesThread()
        {
            // Once N devices are attempting to connect, wait until they are done
            var pendingTasksLimit = this.simulationConcurrencyConfig.MaxPendingConnections;
            var tasks = new List<Task>();

            while (this.running)
            {
                this.connectionLoopSettings.NewLoop();
                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (var deviceConnectionActor in this.deviceConnectionActors)
                {
                    if (deviceConnectionActor.Value.IsDeleted)
                    {
                        this.DeleteActorsForDevice(deviceConnectionActor.Key);
                    }
                    else
                    {
                        tasks.Add(deviceConnectionActor.Value.RunAsync());
                        if (tasks.Count <= pendingTasksLimit) continue;

                        Task.WaitAll(tasks.ToArray());
                        tasks.Clear();
                    }
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Debug("Device state loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, this.simulationConcurrencyConfig.MinDeviceConnectionLoopDuration);
            }

            // If there are pending tasks...
            if (tasks.Count > 0)
            {
                Task.WaitAll(tasks.ToArray());
                tasks.Clear();
            }
        }

        private void SendTelemetryThread(int threadPosition, int threadCount)
        {
            if (this.deviceTelemetryActors.Count == 0)
            {
                this.log.Warn("There is no telemetry to send, stopping this telemetry thread");
                return;
            }

            /**
             * Examples:
             *    threadCount = 3
             *
             *    Count = 20000
             *    chunkSize = 6667
             *    threadPosition 1:     0,  6667
             *    threadPosition 2:  6667, 13334
             *    threadPosition 3: 13334, 20000
             *
             *    Count = 11
             *    chunkSize = 4
             *    threadPosition 1: 0,  4
             *    threadPosition 2: 4,  8
             *    threadPosition 3: 8, 11
             */
            int chunkSize = (int)Math.Ceiling(this.deviceTelemetryActors.Count / (double)threadCount);
            var firstDevice = chunkSize * (threadPosition - 1);
            var lastDevice = Math.Min(chunkSize * threadPosition, this.deviceTelemetryActors.Count);

            // Once N devices are attempting to send telemetry, wait until they are done
            var pendingTasksLimit = this.simulationConcurrencyConfig.MaxPendingTelemetry;
            var tasks = new List<Task>();

            while (this.running)
            {
                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var pos = 0;
                foreach (var telemetry in this.deviceTelemetryActors)
                {
                    // Work only on a subset of all devices
                    if (!(pos >= firstDevice && pos < lastDevice))
                    {
                        tasks.Add(telemetry.Value.RunAsync());
                        if (tasks.Count <= pendingTasksLimit) continue;

                        Task.WaitAll(tasks.ToArray());
                        tasks.Clear();
                    }

                    pos++;
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Debug("Telemetry loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, this.simulationConcurrencyConfig.MinDeviceTelemetryLoopDuration);
            }

            // If there are pending tasks...
            if (tasks.Count > 0)
            {
                Task.WaitAll(tasks.ToArray());
                tasks.Clear();
            }
        }

        private void UpdatePropertiesThread()
        {
            // Once N devices are attempting to write twins, wait until they are done
            var pendingTasksLimit = this.simulationConcurrencyConfig.MaxPendingTwinWrites;
            var tasks = new List<Task>();

            while (this.running)
            {
                this.propertiesLoopSettings.NewLoop();

                var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (var device in this.devicePropertiesActors)
                {
                    tasks.Add(device.Value.RunAsync());
                    if (tasks.Count <= pendingTasksLimit) continue;

                    Task.WaitAll(tasks.ToArray());
                    tasks.Clear();
                }

                var durationMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - before;
                this.log.Debug("Device properties loop completed", () => new { durationMsecs });
                this.SlowDownIfTooFast(durationMsecs, this.simulationConcurrencyConfig.MinDevicePropertiesLoopDuration);
            }

            // If there are pending tasks...
            if (tasks.Count > 0)
            {
                Task.WaitAll(tasks.ToArray());
                tasks.Clear();
            }
        }

        private void SlowDownIfTooFast(long duration, int min)
        {
            // Avoid 1msec sleeps
            if (duration >= min || min - duration <= 1) return;

            var pauseMsecs = min - (int)duration;
            this.log.Debug("Pausing", () => new { pauseMsecs });
            Thread.Sleep(pauseMsecs);
        }

        /**
         * For each device create one actor to periodically update the internal state,
         * one actor to manage the connection to the hub, and one actor for each
         * telemetry message to send.
         */
        private async Task CreateActorsForDeviceAsync(DeviceModel deviceModel, int position, int total, string deviceId = null)
        {
            var id = deviceId ?? this.devices.GenerateId(deviceModel.Id, position);
            var key = deviceId ?? deviceModel.Id + "#" + position;

            this.log.Debug("Creating device actors...",
                () => new { ModelName = deviceModel.Name, ModelId = deviceModel.Id, position });

            // Create one state actor for each device
            var deviceStateActor = this.factory.Resolve<IDeviceStateActor>();
            deviceStateActor.Setup(id, deviceModel, position, total);
            this.deviceStateActors.Add(key, deviceStateActor);

            // Create one connection actor for each device
            var deviceConnectionActor = this.factory.Resolve<IDeviceConnectionActor>();
            await deviceConnectionActor.SetupAsync(id, deviceModel, deviceStateActor, this.connectionLoopSettings);
            this.deviceConnectionActors.Add(key, deviceConnectionActor);

            // Create one device properties actor for each device
            var devicePropertiesActor = this.factory.Resolve<IDevicePropertiesActor>();
            devicePropertiesActor.Setup(id, deviceStateActor, deviceConnectionActor, this.propertiesLoopSettings);
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
                deviceTelemetryActor.Setup(id, deviceModel, message, deviceStateActor, deviceConnectionActor);

                var actorKey = key + "#" + (i++).ToString();
                this.deviceTelemetryActors.Add(actorKey, deviceTelemetryActor);
            }
        }

        private void TryToStopTelemetryThreads()
        {
            for (int i = 0; i < this.simulationConcurrencyConfig.TelemetryThreads; i++)
            {
                try
                {
                    this.devicesTelemetryThreads[i].Interrupt();
                }
                catch (Exception e)
                {
                    this.log.Warn("Unable to stop the telemetry thread in a clean way", () => new { threadNumber = i, e });
                }
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
                this.log.Warn("Unable to stop the connections thread in a clean way", e);
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
                this.log.Warn("Unable to stop the devices state thread in a clean way", e);
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
                this.log.Warn("Unable to stop the devices state thread in a clean way", e);
            }
        }

        private void TryToStartTelemetryThreads()
        {
            try
            {
                var count = this.simulationConcurrencyConfig.TelemetryThreads;

                this.devicesTelemetryThreads = new Thread[count];
                for (int i = 0; i < count; i++)
                {
                    this.devicesTelemetryThreads[i] = new Thread(() => this.SendTelemetryThread(i + 1, count));
                    this.devicesTelemetryThreads[i].Start();
                }
            }
            catch (Exception e)
            {
                var msg = "Unable to start the telemetry threads";
                this.IncrementSimulationErrorsCount();
                this.log.Error(msg, e);
                this.diagnosticsLogger.LogServiceError(msg, e.Message);
                throw new Exception(msg, e);
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
                var msg = "Unable to start the device connection thread";
                this.IncrementSimulationErrorsCount();
                this.log.Error(msg, e);
                this.diagnosticsLogger.LogServiceError(msg, e.Message);
                throw new Exception(msg, e);
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
                var msg = "Unable to start the device state thread";
                this.IncrementSimulationErrorsCount();
                this.log.Error(msg, e);
                this.diagnosticsLogger.LogServiceError(msg, e.Message);
                throw new Exception(msg, e);
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
                var msg = "Unable to start the device properties thread";
                this.IncrementSimulationErrorsCount();
                this.log.Error(msg, e);
                this.diagnosticsLogger.LogServiceError(msg, e.Message);
                throw new Exception(msg, e);
            }
        }

        private void DeleteActorsForDevice(string key)
        {
            var deviceId = this.deviceConnectionActors[key].Device.Id;

            this.log.Info("Remove connection actor for device id ", () => new { key });
            this.deviceConnectionActors.Remove(key);

            foreach (var actor in this.deviceTelemetryActors)
            {
                if (actor.Value.Client.DeviceId.Equals(deviceId))
                {
                    this.log.Info("Stop telemetry actor for deviceId ", () => new { key });
                    actor.Value.Stop();

                    this.log.Info("Remove telemetry actor for deviceId", () => new { key });
                    this.deviceTelemetryActors.Remove(actor.Key);
                }
            }

            this.log.Info("Stop property actor for deviceId ", () => new { key });
            this.devicePropertiesActors[key].Stop();

            this.log.Info("Remove property actor for deviceId", () => new { key });
            this.devicePropertiesActors.Remove(key);

            this.log.Info("Remove state actor for deviceId", () => new { key });
            this.deviceStateActors.Remove(key);
        }

        private void IncrementSimulationErrorsCount()
        {
            Interlocked.Increment(ref this.simulationErrors);
        }

        private async Task AddCustomDeviceAsync(string deviceId, string modelId)
        {
            DeviceModel model = this.GetDeviceModel(modelId, null);
            await this.CreateActorsForDeviceAsync(model, 0, 1, deviceId);
        }
    }
}
