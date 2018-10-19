﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties
{
    public interface IDevicePropertiesActor
    {
        ISmartDictionary DeviceProperties { get; }
        ISmartDictionary DeviceState { get; }
        IDeviceClient Client { get; }
        long FailedTwinUpdatesCount { get; }
        long SimulationErrorsCount { get; }

        void Init(
            ISimulationContext simulationContext,
            string deviceId,
            IDeviceStateActor deviceStateActor,
            IDeviceConnectionActor context,
            PropertiesLoopSettings loopSettings);

        bool HasWorkToDo();
        Task<string> RunAsync();
        void HandleEvent(DevicePropertiesActor.ActorEvents e);
        void Stop();
    }

    /// <summary>
    /// The Device Properties Actor is responsible for sending updates
    /// to the IoT Hub Device Twin. This includes adding an initial tag to 
    /// the device twin, and pushing changes to the device properties.
    /// </summary>
    public class DevicePropertiesActor : IDevicePropertiesActor
    {
        private enum ActorStatus
        {
            None,
            ReadyToStart,
            ReadyToTagDevice,
            TaggingDevice,
            WaitingForChanges,
            ReadyToUpdate,
            Updating,
            Stopped
        }

        public enum ActorEvents
        {
            Started,
            DeviceTaggingFailed,
            DeviceTagged,
            PropertiesUpdateFailed,
            PropertiesUpdated,
        }

        private readonly ILogger log;
        private readonly IActorsLogger actorLogger;
        private readonly IRateLimiting rateLimiting;
        private readonly IDevicePropertiesLogic updatePropertiesLogic;
        private readonly IDevicePropertiesLogic deviceSetDeviceTagLogic;
        private readonly IInstance instance;

        private ActorStatus status;
        private string deviceId;
        private long whenToRun;
        private PropertiesLoopSettings loopSettings;
        private long failedTwinUpdatesCount;
        private ISimulationContext simulationContext;

        /// <summary>
        /// Reference to the actor managing the device state, used
        /// to retrieve the state and prepare the telemetry messages
        /// </summary>
        private IDeviceStateActor deviceStateActor;

        /// <summary>
        /// Reference to the actor managing the device connection
        /// </summary>
        private IDeviceConnectionActor deviceContext;

        /// <summary>
        /// Device properties maintained by the device state actor
        /// </summary>
        public ISmartDictionary DeviceProperties => this.deviceStateActor.DeviceProperties;

        /// <summary>
        /// Device state maintained by the device state actor
        /// </summary>
        public ISmartDictionary DeviceState => this.deviceStateActor.DeviceState;

        /// <summary>
        /// Azure IoT Hub client
        /// </summary>
        public IDeviceClient Client => this.deviceContext.Client;

        /// <summary>
        /// Failed device twin updates counter
        /// </summary>
        public long FailedTwinUpdatesCount => this.failedTwinUpdatesCount;

        /// <summary>
        /// Simulation error counter in DeviceConnectionActor
        /// </summary>
        public long SimulationErrorsCount => this.FailedTwinUpdatesCount;

        public DevicePropertiesActor(
            ILogger logger,
            IActorsLogger actorLogger,
            IRateLimiting rateLimiting,
            UpdateReportedProperties updatePropertiesLogic,
            SetDeviceTag deviceSetDeviceTagLogic,
            IInstance instance)
        {
            this.log = logger;
            this.actorLogger = actorLogger;
            this.rateLimiting = rateLimiting;
            this.updatePropertiesLogic = updatePropertiesLogic;
            this.deviceSetDeviceTagLogic = deviceSetDeviceTagLogic;
            this.instance = instance;

            this.status = ActorStatus.None;
            this.deviceId = null;
            this.deviceStateActor = null;
            this.deviceContext = null;

            this.failedTwinUpdatesCount = 0;
        }

        /// <summary>
        /// Invoke this method before calling Execute(), to initialize the actor
        /// with details like the device id.
        /// </summary>
        public void Init(
            ISimulationContext simulationContext,
            string deviceId,
            IDeviceStateActor deviceStateActor,
            IDeviceConnectionActor context,
            PropertiesLoopSettings loopSettings)
        {
            this.instance.InitOnce();

            this.simulationContext = simulationContext;
            this.deviceId = deviceId;
            this.deviceStateActor = deviceStateActor;
            this.deviceContext = context;
            this.updatePropertiesLogic.Init(this, this.deviceId);
            this.actorLogger.Init(deviceId, "Properties");
            this.status = ActorStatus.ReadyToStart;
            this.loopSettings = loopSettings;

            this.instance.InitComplete();
        }

        public void HandleEvent(ActorEvents e)
        {
            switch (e)
            {
                case ActorEvents.Started:
                    this.actorLogger.ActorStarted();

                    /**
                     * TODO: Devices should be tagged when created through bulk import.
                     *       Remove tagging logic.
                     */
                    // TEMP DISABLED: this.status = ActorStatus.ReadyToTagDevice;
                    this.status = ActorStatus.WaitingForChanges;
                    break;

                case ActorEvents.DeviceTagged:
                    this.actorLogger.DeviceTagged();
                    this.status = ActorStatus.WaitingForChanges;
                    break;

                case ActorEvents.DeviceTaggingFailed:
                    if (this.loopSettings.SchedulableTaggings <= 0) return;
                    this.loopSettings.SchedulableTaggings--;

                    this.failedTwinUpdatesCount++;
                    this.actorLogger.DeviceTaggingFailed();
                    this.ScheduleDeviceTagging();
                    break;

                case ActorEvents.PropertiesUpdated:
                    this.actorLogger.DevicePropertiesUpdated();
                    this.status = ActorStatus.WaitingForChanges;
                    break;

                case ActorEvents.PropertiesUpdateFailed:
                    this.failedTwinUpdatesCount++;
                    this.actorLogger.DevicePropertiesUpdateFailed();
                    this.SchedulePropertiesUpdate(isRetry: true);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e, null);
            }
        }

        public bool HasWorkToDo()
        {
            switch (this.status)
            {
                case ActorStatus.ReadyToStart:
                    return this.deviceContext.Connected;

                case ActorStatus.WaitingForChanges:
                    return this.DeviceProperties?.Changed ?? false;

                case ActorStatus.ReadyToUpdate:
                    return true;
            }

            return false;
        }

        // Run the next step and return a description about what happened
        public async Task<string> RunAsync()
        {
            this.log.Debug(this.status.ToString(), () => new { this.deviceId });

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now < this.whenToRun) return null;

            switch (this.status)
            {
                case ActorStatus.ReadyToStart:
                    if (!this.deviceContext.Connected) return "device not connected yet";
                    this.whenToRun = 0;
                    this.HandleEvent(ActorEvents.Started);
                    return "started";

                case ActorStatus.ReadyToTagDevice:
                    this.status = ActorStatus.TaggingDevice;
                    this.actorLogger.TaggingDevice();
                    await this.deviceSetDeviceTagLogic.RunAsync();
                    return "device tag scheduled";

                case ActorStatus.WaitingForChanges:
                    if (!(this.DeviceProperties?.Changed ?? false)) return "no properties to update";
                    this.SchedulePropertiesUpdate();
                    return "properties update scheduled";

                case ActorStatus.ReadyToUpdate:
                    this.status = ActorStatus.Updating;
                    this.actorLogger.UpdatingDeviceProperties();
                    await this.updatePropertiesLogic.RunAsync();
                    return "updated properties";
            }

            return null;
        }

        public void Stop()
        {
            this.log.Debug("Device properties actor stopped",
                () => new { this.deviceId, Status = this.status.ToString() });

            this.status = ActorStatus.Stopped;
        }

        private void SchedulePropertiesUpdate(bool isRetry = false)
        {
            // considering the throttling settings, when can the properties be updated
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var pauseMsec = this.rateLimiting.GetPauseForNextTwinWrite();
            this.whenToRun = now + pauseMsec;
            this.status = ActorStatus.ReadyToUpdate;

            this.actorLogger.DevicePropertiesUpdateScheduled(this.whenToRun, isRetry);
            this.log.Debug("Device properties update scheduled",
                () => new
                {
                    this.deviceId,
                    Status = this.status.ToString(),
                    When = this.log.FormatDate(this.whenToRun),
                    isRetry
                });
        }

        private void ScheduleDeviceTagging()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // note: we overwrite the twin, so no Read operation is needed
            var pauseMsec = this.rateLimiting.GetPauseForNextTwinWrite();
            this.whenToRun = now + pauseMsec;
            this.status = ActorStatus.ReadyToTagDevice;

            this.actorLogger.DeviceTaggingScheduled(this.whenToRun);
            this.log.Debug("Device twin tagging scheduled",
                () => new
                {
                    this.deviceId,
                    Status = this.status.ToString(),
                    When = this.log.FormatDate(this.whenToRun)
                });
        }
    }
}
