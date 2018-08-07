// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
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
        long FailedDeviceConnectionsCount { get; }
        long SimulationErrorsCount { get; }
        bool IsDeleted { get; }
        void Setup(string deviceId, DeviceModel deviceModel, IDeviceStateActor deviceStateActor, ConnectionLoopSettings loopSettings);
        Task RunAsync();
        void HandleEvent(DeviceConnectionActor.ActorEvents e);
        void Stop();
        void Delete();
    }

    public class DeviceConnectionActor : IDeviceConnectionActor
    {
        public enum ActorStatus
        {
            None,
            ReadyToStart,
            ReadyToSetupCredentials,
            ReadyToFetch,
            PreparingCredentials,
            Fetching,
            ReadyToRegister,
            Registering,
            ReadyToConnect,
            Connecting,
            ReadyToAddToStore,
            AddingToStore,
            Done,
            Stopped,
            ReadyToDisconnect,
            Disconnecting,
            ReadyToDeregister,
            Deregistering,
            ReadyToDeleteFromStore,
            DeletingFromStore,
            Deleted
        }

        public enum ActorEvents
        {
            Started,
            DeviceNotFound,
            FetchFailed,
            FetchCompleted,
            RegistrationFailed,
            CredentialsSetupCompleted,
            DeviceRegistered,
            DeviceDeregistered,
            DeregisterationFailed,
            AddToStoreCompleted,
            AddToStoreFailed,
            Connected,
            ConnectionFailed,
            DeleteFromStoreCompleted,
            DeleteFromStoreFailed,
            DisconnectionFailed,
            Disconnected,
            AuthFailed,
            TelemetryClientBroken
        }

        private readonly ILogger log;
        private readonly IActorsLogger actorLogger;
        private readonly IRateLimiting rateLimiting;
        private readonly IDeviceConnectionLogic fetchFromRegistryLogic;
        private readonly IDeviceConnectionLogic credentialsSetupLogic;
        private readonly IDeviceConnectionLogic registerLogic;
        private readonly IDeviceConnectionLogic connectLogic;
        private readonly IDeviceConnectionLogic deregisterLogic;
        private readonly IDeviceConnectionLogic disconnectLogic;
        private readonly IDeviceConnectionLogic deleteFromStoreLogic;
        private readonly IDeviceConnectionLogic addToStoreLogic;

        private ActorStatus status;
        private string deviceId;
        private DeviceModel deviceModel;
        private long whenToRun;
        private ConnectionLoopSettings loopSettings;
        private long failedDeviceConnectionsCount;
        private long failedRegistrationsCount;
        private long failedFetchCount;

        /// <summary>
        /// Reference to the actor managing the device state, used
        /// to retrieve the state and prepare the telemetry messages
        /// </summary>
        private IDeviceStateActor deviceStateActor;

        /// <summary>
        /// Device state maintained by the device state actor
        /// </summary>
        public ISmartDictionary DeviceState => this.deviceStateActor.DeviceState;

        /// <summary>
        /// Device properties maintained by the device state actor
        /// </summary>
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

        /// <summary>
        /// Failed device connections counter
        /// </summary>
        public long FailedDeviceConnectionsCount => this.failedDeviceConnectionsCount;

        /// <summary>
        /// Device is deleted
        /// </summary>
        public bool IsDeleted => this.status == ActorStatus.Deleted;

        /// <summary>
        /// Simulation error counter in DeviceConnectionActor
        /// </summary>
        public long SimulationErrorsCount => this.failedRegistrationsCount +
                                             this.failedFetchCount +
                                             this.FailedDeviceConnectionsCount;

        public DeviceConnectionActor(
            ILogger logger,
            IActorsLogger actorLogger,
            IRateLimiting rateLimiting,
            CredentialsSetup credentialsSetupLogic,
            AddToStore addToStoreLogic,
            FetchFromRegistry fetchFromRegistryLogic,
            Register registerLogic,
            Connect connectLogic,
            Deregister deregisterLogic,
            Disconnect disconnectLogic,
            DeleteFromStore deleteFromStoreLogic)
        {
            this.log = logger;
            this.actorLogger = actorLogger;
            this.rateLimiting = rateLimiting;

            this.credentialsSetupLogic = credentialsSetupLogic;
            this.addToStoreLogic = addToStoreLogic;
            this.fetchFromRegistryLogic = fetchFromRegistryLogic;
            this.registerLogic = registerLogic;
            this.connectLogic = connectLogic;
            this.deregisterLogic = deregisterLogic;
            this.disconnectLogic = disconnectLogic;
            this.deleteFromStoreLogic = deleteFromStoreLogic;

            this.Message = null;
            this.Client = null;
            this.Device = null;

            this.status = ActorStatus.None;
            this.deviceModel = null;
            this.deviceId = null;
            this.deviceStateActor = null;

            this.failedDeviceConnectionsCount = 0;
            this.failedRegistrationsCount = 0;
            this.failedFetchCount = 0;
        }

        /// <summary>
        /// Invoke this method before calling Execute(), to initialize the actor
        /// with details like the device model and message type to simulate.
        /// Setup() should be called only once.
        /// </summary>
        public void Setup(
            string deviceId,
            DeviceModel deviceModel,
            IDeviceStateActor deviceStateActor,
            ConnectionLoopSettings loopSettings)
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

            this.credentialsSetupLogic.Setup(this, this.deviceId, this.deviceModel);
            this.addToStoreLogic.Setup(this, this.deviceId, this.deviceModel);
            this.fetchFromRegistryLogic.Setup(this, this.deviceId, this.deviceModel);
            this.registerLogic.Setup(this, this.deviceId, this.deviceModel);
            this.connectLogic.Setup(this, this.deviceId, this.deviceModel);
            this.deregisterLogic.Setup(this, this.deviceId, this.deviceModel);
            this.disconnectLogic.Setup(this, this.deviceId, this.deviceModel);
            this.deleteFromStoreLogic.Setup(this, this.deviceId, this.deviceModel);
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

        public void Delete()
        {
            try
            {
                // this.status = ActorStatus.ReadyToDisconnect;
                this.ScheduleDisconnection();
                this.actorLogger.DisconnectingDevice();
            }
            catch (Exception e)
            {
                this.log.Warn("Error while deleting", () => new { e });
            }
        }

        public async Task RunAsync()
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

                case ActorStatus.ReadyToSetupCredentials:
                    this.status = ActorStatus.PreparingCredentials;
                    this.actorLogger.PreparingDeviceCredentials();
                    await this.credentialsSetupLogic.RunAsync();
                    return;

                case ActorStatus.ReadyToFetch:
                    this.status = ActorStatus.Fetching;
                    this.actorLogger.FetchingDevice();
                    await this.fetchFromRegistryLogic.RunAsync();
                    return;

                case ActorStatus.ReadyToRegister:
                    this.status = ActorStatus.Registering;
                    this.actorLogger.RegisteringDevice();
                    await this.registerLogic.RunAsync();
                    return;

                case ActorStatus.ReadyToConnect:
                    this.status = ActorStatus.Connecting;
                    this.actorLogger.ConnectingDevice();
                    await this.connectLogic.RunAsync();
                    return;

                case ActorStatus.ReadyToDeregister:
                    this.status = ActorStatus.Deregistering;
                    this.actorLogger.DeregisteringDevice();
                    await this.deregisterLogic.RunAsync();
                    return;

                case ActorStatus.ReadyToDisconnect:
                    this.status = ActorStatus.Disconnecting;
                    this.actorLogger.DisconnectingDevice();
                    await this.disconnectLogic.RunAsync();
                    return;

                case ActorStatus.ReadyToAddToStore:
                    this.status = ActorStatus.AddingToStore;
                    this.actorLogger.AddingDeviceToStore();
                    await this.addToStoreLogic.RunAsync();
                    return;

                case ActorStatus.ReadyToDeleteFromStore:
                    this.status = ActorStatus.DeletingFromStore;
                    this.actorLogger.DeletingDeviceFromStore();
                    await this.deleteFromStoreLogic.RunAsync();
                    return;
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
                    this.ScheduleCredentialsSetup();
                    break;

                case ActorEvents.FetchFailed:
                    if (this.loopSettings.SchedulableFetches <= 0) return;
                    this.loopSettings.SchedulableFetches--;

                    this.failedFetchCount++;
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
                    this.actorLogger.DeviceRegistered();
                    this.ScheduleConnection();
                    break;

                case ActorEvents.RegistrationFailed:
                    if (this.loopSettings.SchedulableRegistrations <= 0) return;
                    this.loopSettings.SchedulableRegistrations--;

                    this.failedRegistrationsCount++;
                    this.actorLogger.DeviceRegistrationFailed();
                    this.ScheduleRegistration();
                    break;

                case ActorEvents.CredentialsSetupCompleted:
                    this.actorLogger.DeviceCredentialsReady();
                    this.ScheduleConnection();
                    break;

                case ActorEvents.FetchCompleted:
                    this.actorLogger.DeviceFetched();
                    this.ScheduleConnection();
                    break;

                case ActorEvents.AuthFailed:
                    this.actorLogger.DeviceConnectionAuthFailed();
                    this.ScheduleFetch();
                    break;

                case ActorEvents.ConnectionFailed:
                    this.failedDeviceConnectionsCount++;
                    this.actorLogger.DeviceConnectionFailed();
                    this.ScheduleConnection();
                    break;

                case ActorEvents.Connected:
                    this.actorLogger.DeviceConnected();
                    this.ScheduleAddToStore();
                    break;

                case ActorEvents.AddToStoreCompleted:
                    this.actorLogger.AddedDeviceToStore();
                    this.status = ActorStatus.Done;
                    break;

                case ActorEvents.DeleteFromStoreCompleted:
                    this.actorLogger.DeletedDeviceFromStore();
                    this.ScheduleDeregistration();
                    break;

                case ActorEvents.Disconnected:
                    this.actorLogger.DeviceDisconnected();
                    this.ScheduleDeleteFromStore();
                    break;

                case ActorEvents.DeviceDeregistered:
                    this.actorLogger.DeviceDeregistered();
                    this.status = ActorStatus.Deleted;
                    break;

                case ActorEvents.TelemetryClientBroken:
                    this.Client?.DisconnectAsync();
                    this.Client = null;
                    this.ScheduleConnection();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e, null);
            }
        }

        private void ScheduleCredentialsSetup()
        {
            this.whenToRun = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.status = ActorStatus.ReadyToSetupCredentials;

            this.actorLogger.CredentialsSetupScheduled(this.whenToRun);
            this.log.Debug("Device credentials setup scheduled",
                () => new
                {
                    this.deviceId,
                    Status = this.status.ToString(),
                    When = this.log.FormatDate(this.whenToRun)
                });
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

        private void ScheduleAddToStore()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var pauseMsec = this.rateLimiting.GetPauseForNextRegistryOperation();
            this.whenToRun = now + pauseMsec;
            this.status = ActorStatus.ReadyToAddToStore;

            this.actorLogger.AddDeviceToStoreScheduled(this.whenToRun);
            this.log.Debug("Add to store scheduled",
                () => new
                {
                    this.deviceId,
                    Status = this.status.ToString(),
                    When = this.log.FormatDate(this.whenToRun)
                });
        }

        private void ScheduleDeleteFromStore()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var pauseMsec = this.rateLimiting.GetPauseForNextConnection();
            this.whenToRun = now + pauseMsec;
            this.status = ActorStatus.ReadyToDeleteFromStore;

            this.actorLogger.DeleteDeviceFromStoreScheduled(this.whenToRun);
            this.log.Debug("Delete from store scheduled",
                () => new
                {
                    this.deviceId,
                    Status = this.status.ToString(),
                    When = this.log.FormatDate(this.whenToRun)
                });
        }

        private void ScheduleDeregistration()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var pauseMsec = this.rateLimiting.GetPauseForNextRegistryOperation();
            this.whenToRun = now + pauseMsec;
            this.status = ActorStatus.ReadyToDeregister;

            this.actorLogger.DeregistrationScheduled(this.whenToRun);
            this.log.Debug("Deregistration scheduled",
                () => new
                {
                    this.deviceId,
                    Status = this.status.ToString(),
                    When = this.log.FormatDate(this.whenToRun)
                });
        }

        private void ScheduleDisconnection()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var pauseMsec = this.rateLimiting.GetPauseForNextConnection();
            this.whenToRun = now + pauseMsec;
            this.status = ActorStatus.ReadyToDisconnect;

            this.actorLogger.DeviceDisconnectionScheduled(this.whenToRun);
            this.log.Debug("Disconnection scheduled",
                () => new
                {
                    this.deviceId,
                    Status = this.status.ToString(),
                    When = this.log.FormatDate(this.whenToRun)
                });
        }
    }
}
