// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    public interface IDeviceConnectionActor
    {
        ISmartDictionary DeviceState { get; }
        ISmartDictionary DeviceProperties { get; }
        IDeviceClient Client { get; set; }
        Device Device { get; set; }
        bool Connected { get; }

        void Setup(string deviceId, DeviceModel deviceModel, IDeviceStateActor deviceStateActor, ConnectionLoopSettings loopSettings);
        void Run();
        void HandleEvent(DeviceConnectionActor.ActorEvents e);
        void Stop();
    }

    /**
     * TODO: when the device exists already, check whether it is tagged
     */
    public class DeviceConnectionActor : IDeviceConnectionActor
    {
        private enum ActorStatus
        {
            None,
            ReadyToStart,
            ReadyToFetch,
            Fetching,
            ReadyToRegister,
            Registering,
            ReadyToTagDeviceTwin,
            TaggingDeviceTwin,
            ReadyToConnect,
            Connecting,
            Done,
            Stopped
        }

        public enum ActorEvents
        {
            Started,
            DeviceNotFound,
            FetchFailed,
            FetchCompleted,
            RegistrationFailed,
            DeviceRegistered,
            DeviceTwinTaggingFailed,
            DeviceTwinTagged,
            Connected,
            ConnectionFailed
        }

        private readonly ILogger log;
        private readonly IActorsLogger actorLogger;
        private readonly IRateLimiting rateLimiting;
        private readonly IDeviceConnectionLogic fetchLogic;
        private readonly IDeviceConnectionLogic registerLogic;
        private readonly IDeviceConnectionLogic deviceTwinTagLogic;
        private readonly IDeviceConnectionLogic connectLogic;

        private ActorStatus status;
        private string deviceId;
        private DeviceModel deviceModel;
        private long whenToRun;
        private ConnectionLoopSettings loopSettings;

        /// <summary>
        /// Reference to the actor managing the device state, used
        /// to retrieve the state and prepare the telemetry messages
        /// </summary>
        private IDeviceStateActor deviceStateActor;

        public ISmartDictionary DeviceState => this.deviceStateActor.DeviceState;
        public ISmartDictionary DeviceProperties => this.deviceStateActor.DeviceProperties;

        /// <summary>
        /// Azure IoT Hub client shared by Connect, Properties, and SendTelemetry
        /// </summary>
        public IDeviceClient Client { get; set; }

        /// <summary>
        /// Azure IoT Hub Device instance
        /// </summary>
        public Device Device { get; set; }

        /// <summary>
        /// Whether the connection is established
        /// </summary>
        public bool Connected => this.status == ActorStatus.Done;

        /// <summary>
        /// The telemetry message managed by this actor
        /// </summary>
        public DeviceModel.DeviceModelMessage Message { get; private set; }

        public DeviceConnectionActor(
            ILogger logger,
            IActorsLogger actorLogger,
            IRateLimiting rateLimiting,
            Fetch fetchLogic,
            Register registerLogic,
            DeviceTwinTag deviceTwinTagLogic,
            Connect connectLogic)
        {
            this.log = logger;
            this.actorLogger = actorLogger;
            this.rateLimiting = rateLimiting;

            this.fetchLogic = fetchLogic;
            this.registerLogic = registerLogic;
            this.deviceTwinTagLogic = deviceTwinTagLogic;
            this.connectLogic = connectLogic;

            this.Message = null;
            this.Client = null;
            this.Device = null;

            this.status = ActorStatus.None;
            this.deviceModel = null;
            this.deviceId = null;
            this.deviceStateActor = null;
        }

        /// <summary>
        /// Invoke this method before calling Execute(), to initialize the actor
        /// with details like the device model and message type to simulate.
        /// Setup() should be called only once.
        /// </summary>
        public void Setup(string deviceId, DeviceModel deviceModel, IDeviceStateActor deviceStateActor, ConnectionLoopSettings loopSettings)
        {
            if (this.status != ActorStatus.None)
            {
                this.log.Error("The actor is already initialized",
                    () => new { CurrentDeviceId = this.deviceId, NewDeviceModelName = deviceModel.Name });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.deviceModel = deviceModel;
            this.deviceId = deviceId;
            this.deviceStateActor = deviceStateActor;
            this.loopSettings = loopSettings;

            this.fetchLogic.Setup(this, this.deviceId, this.deviceModel);
            this.registerLogic.Setup(this, this.deviceId, this.deviceModel);
            this.deviceTwinTagLogic.Setup(this, this.deviceId, this.deviceModel);
            this.connectLogic.Setup(this, this.deviceId, this.deviceModel);
            this.actorLogger.Setup(deviceId, "Connection");

            this.status = ActorStatus.ReadyToStart;
        }

        public void Stop()
        {
            try
            {
                this.status = ActorStatus.Stopped;
                this.actorLogger.ActorStopped();
                this.Client?.DisconnectAsync();
            }
            catch (Exception e)
            {
                this.log.Warn("Error while stopping", () => new { e });
            }
        }

        public void HandleEvent(ActorEvents e)
        {
            switch (e)
            {
                case ActorEvents.Started:
                    if (this.loopSettings.SchedulableFetches <= 0) return;
                    this.loopSettings.SchedulableFetches--;

                    this.actorLogger.ActorStarted();
                    this.ScheduleFetch();
                    break;

                case ActorEvents.FetchFailed:
                    if (this.loopSettings.SchedulableFetches <= 0) return;
                    this.loopSettings.SchedulableFetches--;

                    this.actorLogger.DeviceFetchFailed();
                    this.ScheduleFetch();
                    break;

                case ActorEvents.DeviceNotFound:
                    if (this.loopSettings.SchedulableRegistrations <= 0) return;
                    this.loopSettings.SchedulableRegistrations--;

                    this.actorLogger.DeviceNotFound();
                    this.ScheduleRegistration();
                    break;

                case ActorEvents.DeviceRegistered:
                    if (this.loopSettings.SchedulableTaggings <= 0) return;
                    this.loopSettings.SchedulableTaggings--;

                    this.actorLogger.DeviceRegistered();
                    this.ScheduleDeviceTagging();
                    break;

                case ActorEvents.RegistrationFailed:
                    if (this.loopSettings.SchedulableRegistrations <= 0) return;
                    this.loopSettings.SchedulableRegistrations--;

                    this.actorLogger.DeviceRegistrationFailed();
                    this.ScheduleRegistration();
                    break;

                case ActorEvents.DeviceTwinTaggingFailed:
                    if (this.loopSettings.SchedulableTaggings <= 0) return;
                    this.loopSettings.SchedulableTaggings--;

                    this.actorLogger.DeviceTwinTaggingFailed();
                    this.ScheduleDeviceTagging();
                    break;

                case ActorEvents.FetchCompleted:
                    this.actorLogger.DeviceFetched();
                    this.ScheduleConnection();
                    break;

                case ActorEvents.DeviceTwinTagged:
                    this.actorLogger.DeviceTwinTagged();
                    this.ScheduleConnection();
                    break;

                case ActorEvents.ConnectionFailed:
                    this.actorLogger.DeviceConnectionFailed();
                    this.ScheduleConnection();
                    break;

                case ActorEvents.Connected:
                    this.actorLogger.DeviceConnected();
                    this.status = ActorStatus.Done;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e, null);
            }
        }

        public void Run()
        {
            this.log.Debug(this.status.ToString(), () => new { this.deviceId });

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now < this.whenToRun) return;

            switch (this.status)
            {
                case ActorStatus.ReadyToStart:
                    this.whenToRun = 0;
                    this.HandleEvent(ActorEvents.Started);
                    return;

                case ActorStatus.ReadyToFetch:
                    this.status = ActorStatus.Fetching;
                    this.actorLogger.FetchingDevice();
                    this.fetchLogic.RunAsync();
                    return;

                case ActorStatus.ReadyToRegister:
                    this.status = ActorStatus.Registering;
                    this.actorLogger.RegisteringDevice();
                    this.registerLogic.RunAsync();
                    return;

                case ActorStatus.ReadyToConnect:
                    this.status = ActorStatus.Connecting;
                    this.actorLogger.ConnectingDevice();
                    this.connectLogic.RunAsync();
                    return;

                case ActorStatus.ReadyToTagDeviceTwin:
                    this.status = ActorStatus.TaggingDeviceTwin;
                    this.actorLogger.TaggingDeviceTwin();
                    this.deviceTwinTagLogic.RunAsync();
                    return;
            }
        }

        private void ScheduleFetch()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var pauseMsec = this.rateLimiting.GetPauseForNextRegistryOperation();
            this.whenToRun = now + pauseMsec;
            this.status = ActorStatus.ReadyToFetch;

            this.actorLogger.FetchScheduled(this.whenToRun);
            this.log.Debug("Device fetch scheduled",
                () => new
                {
                    this.deviceId,
                    Status = this.status.ToString(),
                    When = this.log.FormatDate(this.whenToRun)
                });
        }

        private void ScheduleRegistration()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var pauseMsec = this.rateLimiting.GetPauseForNextRegistryOperation();
            this.whenToRun = now + pauseMsec;
            this.status = ActorStatus.ReadyToRegister;

            this.actorLogger.RegistrationScheduled(this.whenToRun);
            this.log.Debug("Registration scheduled",
                () => new
                {
                    this.deviceId,
                    Status = this.status.ToString(),
                    When = this.log.FormatDate(this.whenToRun)
                });
        }

        private void ScheduleDeviceTagging()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // note: we overwrite the twin, so no Read operation is needed
            var pauseMsec = this.rateLimiting.GetPauseForNextTwinWrite();
            this.whenToRun = now + pauseMsec;
            this.status = ActorStatus.ReadyToTagDeviceTwin;

            this.actorLogger.DeviceTwinTaggingScheduled(this.whenToRun);
            this.log.Debug("Device twin tagging scheduled",
                () => new
                {
                    this.deviceId,
                    Status = this.status.ToString(),
                    When = this.log.FormatDate(this.whenToRun)
                });
        }

        private void ScheduleConnection()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var pauseMsec = this.rateLimiting.GetPauseForNextConnection();
            this.whenToRun = now + pauseMsec;
            this.status = ActorStatus.ReadyToConnect;

            this.actorLogger.DeviceConnectionScheduled(this.whenToRun);
            this.log.Debug("Connection scheduled",
                () => new
                {
                    this.deviceId,
                    Status = this.status.ToString(),
                    When = this.log.FormatDate(this.whenToRun)
                });
        }
    }
}
