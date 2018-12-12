// Copyright (c) Microsoft. All rights reserved.

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

        // Used by the main thread to decide whether to invoke RunAsync(), in order to
        // reduce the chance of enqueuing an async task when there is nothing to do
        bool HasWorkToDo();

        Task RunAsync();
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
        public enum ActorEvents
        {
            Started,
            DeviceTaggingFailed,
            DeviceTagged,
            PropertiesUpdateFailed,
            PropertiesUpdated,
            PropertiesClientBroken
        }

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

        private readonly ILogger log;
        private readonly IActorsLogger actorLogger;
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

        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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
            UpdateReportedProperties updatePropertiesLogic,
            SetDeviceTag deviceSetDeviceTagLogic,
            IInstance instance)
        {
            this.log = logger;
            this.actorLogger = actorLogger;
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
            this.updatePropertiesLogic.Init(this, this.deviceId, this.simulationContext.Devices);
            this.actorLogger.Init(deviceId, "Properties");
            this.status = ActorStatus.ReadyToStart;
            this.loopSettings = loopSettings;

            this.instance.InitComplete();
        }

        // Used by the main thread to decide whether to invoke RunAsync(), in order to
        // reduce the chance of enqueuing an async task when there is nothing to do
        public bool HasWorkToDo()
        {
            if (Now < this.whenToRun) return false;

            if (!this.deviceContext.Connected) return false;

            switch (this.status)
            {
                case ActorStatus.ReadyToStart:
                case ActorStatus.ReadyToTagDevice:
                case ActorStatus.WaitingForChanges:
                case ActorStatus.ReadyToUpdate:
                    return
                        this.DeviceProperties != null
                        && this.DeviceProperties.Changed
                        && (!this.simulationContext.RateLimiting.HasExceededMessagingQuota
                            || this.simulationContext.RateLimiting.CanProbeMessagingQuota(Now));
            }

            return false;
        }

        public async Task RunAsync()
        {
            this.instance.InitRequired();

            this.log.Debug(this.status.ToString(), () => new { this.deviceId });

            switch (this.status)
            {
                case ActorStatus.ReadyToStart:
                    this.whenToRun = 0;
                    this.HandleEvent(ActorEvents.Started);
                    break;

                case ActorStatus.ReadyToTagDevice:
                    this.status = ActorStatus.TaggingDevice;
                    this.actorLogger.TaggingDevice();
                    await this.deviceSetDeviceTagLogic.RunAsync();
                    break;

                case ActorStatus.WaitingForChanges:
                    this.SchedulePropertiesUpdate();
                    break;

                case ActorStatus.ReadyToUpdate:
                    this.status = ActorStatus.Updating;
                    this.actorLogger.UpdatingDeviceProperties();
                    await this.updatePropertiesLogic.RunAsync();
                    break;
            }
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

                case ActorEvents.PropertiesClientBroken:
                    this.failedTwinUpdatesCount++;
                    this.actorLogger.DevicePropertiesUpdateFailed();
                    this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.PropertiesClientBroken);
                    this.Reset();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e, null);
            }
        }

        public void Stop()
        {
            this.log.Debug("Device properties actor stopped",
                () => new { this.deviceId, Status = this.status.ToString() });

            this.status = ActorStatus.Stopped;
        }

        private void Reset()
        {
            this.status = ActorStatus.ReadyToStart;
        }

        private void SchedulePropertiesUpdate(bool isRetry = false)
        {
            // considering the throttling settings, when can the properties be updated
            var pauseMsec = this.simulationContext.RateLimiting.GetPauseForNextTwinWrite();
            this.whenToRun = Now + pauseMsec;
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
            // note: we overwrite the twin, so no Read operation is needed
            var pauseMsec = this.simulationContext.RateLimiting.GetPauseForNextTwinWrite();
            this.whenToRun = Now + pauseMsec;
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
