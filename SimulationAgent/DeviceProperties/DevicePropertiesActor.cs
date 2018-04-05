// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties
{
    public interface IDevicePropertiesActor
    {
        ISmartDictionary DeviceProperties { get; }
        ISmartDictionary DeviceState { get; }
        IDeviceClient Client { get; }

        void Setup(
            string deviceId,
            IDeviceStateActor deviceStateActor,
            IDeviceConnectionActor deviceConnectionActor);

        string Run();
        void HandleEvent(DevicePropertiesActor.ActorEvents e);
        void Stop();
    }

    public class DevicePropertiesActor : IDevicePropertiesActor
    {
        private enum ActorStatus
        {
            None,
            ReadyToStart,
            WaitingForChanges,
            ReadyToUpdate,
            Updating,
            Stopped
        }

        public enum ActorEvents
        {
            Started,
            PropertiesUpdateFailed,
            PropertiesUpdated,
        }

        private readonly ILogger log;
        private readonly IActorsLogger actorLogger;
        private readonly IRateLimiting rateLimiting;
        private readonly IDevicePropertiesLogic updatePropertiesLogic;

        private ActorStatus status;
        private string deviceId;
        private long whenToRun;

        /// <summary>
        /// Reference to the actor managing the device state, used
        /// to retrieve the state and prepare the telemetry messages
        /// </summary>
        private IDeviceStateActor deviceStateActor;

        /// <summary>
        /// Reference to the actor managing the device connection
        /// </summary>
        private IDeviceConnectionActor deviceConnectionActor;

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
        public IDeviceClient Client => this.deviceConnectionActor.Client;

        public DevicePropertiesActor(
            ILogger logger,
            IActorsLogger actorLogger,
            IRateLimiting rateLimiting,
            IDevicePropertiesLogic updatePropertiesLogic)
        {
            this.log = logger;
            this.actorLogger = actorLogger;
            this.rateLimiting = rateLimiting;
            this.updatePropertiesLogic = updatePropertiesLogic;

            this.status = ActorStatus.None;
            this.deviceId = null;
            this.deviceStateActor = null;
            this.deviceConnectionActor = null;
        }

        /// <summary>
        /// Invoke this method before calling Execute(), to initialize the actor
        /// with details like the device id. Setup() should be called only once.
        /// </summary>
        public void Setup(
            string deviceId,
            IDeviceStateActor deviceStateActor,
            IDeviceConnectionActor deviceConnectionActor)
        {
            if (this.status != ActorStatus.None)
            {
                this.log.Error("The actor is already initialized",
                    () => new { CurrentDeviceId = this.deviceId });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.deviceId = deviceId;
            this.deviceStateActor = deviceStateActor;
            this.deviceConnectionActor = deviceConnectionActor;

            this.updatePropertiesLogic.Setup(this, this.deviceId);
            this.actorLogger.Setup(deviceId, "Properties");

            this.status = ActorStatus.ReadyToStart;
        }

        public void HandleEvent(ActorEvents e)
        {
            switch (e)
            {
                case ActorEvents.Started:
                    this.actorLogger.ActorStarted();
                    break;
                case ActorEvents.PropertiesUpdated:
                    this.actorLogger.DevicePropertiesUpdated();
                    this.status = ActorStatus.WaitingForChanges;
                    break;
                case ActorEvents.PropertiesUpdateFailed:
                    this.actorLogger.DevicePropertiesUpdateFailed();
                    this.SchedulePropertiesUpdate(isRetry: true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e, null);
            }
        }

        // Run the next step and return a description about what happened
        public string Run()
        {
            this.log.Debug(this.status.ToString(), () => new { this.deviceId });

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now < this.whenToRun) return null;

            switch (this.status)
            {
                case ActorStatus.ReadyToStart:
                    if (!this.deviceConnectionActor.Connected) return "device not connected yet";
                    this.whenToRun = 0;
                    this.HandleEvent(ActorEvents.Started);
                    return "started";

                case ActorStatus.WaitingForChanges:
                    if (!this.DeviceProperties.Changed) return "no properties to update";
                    this.SchedulePropertiesUpdate();
                    return "properties update scheduled";

                case ActorStatus.ReadyToUpdate:
                    this.status = ActorStatus.Updating;
                    this.actorLogger.UpdatingDeviceProperties();
                    this.updatePropertiesLogic.Run();
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
            // considering the throttling settings, when can the properties can be updated
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
    }
}
