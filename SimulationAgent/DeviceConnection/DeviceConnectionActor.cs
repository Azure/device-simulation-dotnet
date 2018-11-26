// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection
{
    public interface IDeviceConnectionActor
    {
        ISimulationContext SimulationContext { get; }
        ISmartDictionary DeviceState { get; }
        ISmartDictionary DeviceProperties { get; }
        IDeviceClient Client { get; set; }
        Device Device { get; set; }
        bool Connected { get; }
        long FailedDeviceConnectionsCount { get; }
        long SimulationErrorsCount { get; }
        bool IsDeleted { get; }

        void Init(
            ISimulationContext simulationContext,
            string deviceId,
            DeviceModel deviceModel,
            IDeviceStateActor deviceStateActor,
            ConnectionLoopSettings loopSettings);

        // Used by the main thread to decide whether to invoke RunAsync(), in order to
        // reduce the chance of enqueuing an async task when there is nothing to do
        bool HasWorkToDo();

        Task RunAsync();
        void HandleEvent(DeviceConnectionActor.ActorEvents e);
        void Stop();
        void Delete();
        void DisposeClient();
    }

    public class DeviceConnectionActor : IDeviceConnectionActor
    {
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
            Connected,
            ConnectionFailed,
            DisconnectionFailed,
            Disconnected,
            AuthFailed,
            TelemetryClientBroken,
        }

        private enum ActorStatus
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
            Done,
            Stopped,
            ReadyToDisconnect,
            Disconnecting,
            ReadyToDeregister,
            Deregistering,
            Deleted
        }

        private readonly ILogger log;
        private readonly IActorsLogger actorLogger;
        private readonly IDeviceConnectionLogic fetchFromRegistryLogic;
        private readonly IDeviceConnectionLogic credentialsSetupLogic;
        private readonly IDeviceConnectionLogic registerLogic;
        private readonly IDeviceConnectionLogic connectLogic;
        private readonly IDeviceConnectionLogic deregisterLogic;
        private readonly IDeviceConnectionLogic disconnectLogic;
        private readonly IInstance instance;

        private ActorStatus status;
        private string deviceId;
        private DeviceModel deviceModel;
        private long whenToRun;
        private ConnectionLoopSettings loopSettings;
        private long failedDeviceConnectionsCount;
        private long failedRegistrationsCount;
        private long failedFetchCount;

        /// <summary>
        /// Contains all the simulation specific dependencies, e.g. hub, rating
        /// limits, device registry, etc.
        /// </summary>
        private ISimulationContext simulationContext;

        /// <summary>
        /// Reference to the actor managing the device state, used
        /// to retrieve the state and prepare the telemetry messages
        /// </summary>
        private IDeviceStateActor deviceStateActor;

        /// <summary>
        /// Proxy to all the dependencies initialized for the simulation that the actor
        /// belongs too. E.g. hub registry, conn strings, rating limits, etc.
        /// </summary>
        public ISimulationContext SimulationContext => this.simulationContext;

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
            CredentialsSetup credentialsSetupLogic,
            FetchFromRegistry fetchFromRegistryLogic,
            Register registerLogic,
            Connect connectLogic,
            Deregister deregisterLogic,
            Disconnect disconnectLogic,
            IInstance instance)
        {
            this.log = logger;
            this.actorLogger = actorLogger;

            this.credentialsSetupLogic = credentialsSetupLogic;
            this.fetchFromRegistryLogic = fetchFromRegistryLogic;
            this.registerLogic = registerLogic;
            this.connectLogic = connectLogic;
            this.instance = instance;
            this.deregisterLogic = deregisterLogic;
            this.disconnectLogic = disconnectLogic;

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
        /// Init() should be called only once.
        /// </summary>
        public void Init(
            ISimulationContext simulationContext,
            string deviceId,
            DeviceModel deviceModel,
            IDeviceStateActor deviceStateActor,
            ConnectionLoopSettings loopSettings)
        {
            this.instance.InitOnce();

            this.simulationContext = simulationContext;
            this.deviceModel = deviceModel;
            this.deviceId = deviceId;
            this.deviceStateActor = deviceStateActor;
            this.loopSettings = loopSettings;

            this.credentialsSetupLogic.Init(this, this.deviceId, this.deviceModel);
            this.fetchFromRegistryLogic.Init(this, this.deviceId, this.deviceModel);
            this.registerLogic.Init(this, this.deviceId, this.deviceModel);
            this.connectLogic.Init(this, this.deviceId, this.deviceModel);
            this.deregisterLogic.Init(this, this.deviceId, this.deviceModel);
            this.disconnectLogic.Init(this, this.deviceId, this.deviceModel);
            this.actorLogger.Init(deviceId, "Connection");

            this.status = ActorStatus.ReadyToStart;

            this.instance.InitComplete();
        }

        // Used by the main thread to decide whether to invoke RunAsync(), in order to
        // reduce the chance of enqueuing an async task when there is nothing to do
        public bool HasWorkToDo()
        {
            switch (this.status)
            {
                case ActorStatus.None:
                case ActorStatus.ReadyToStart:
                case ActorStatus.ReadyToSetupCredentials:
                case ActorStatus.ReadyToFetch:
                case ActorStatus.PreparingCredentials:
                case ActorStatus.Fetching:
                case ActorStatus.ReadyToRegister:
                case ActorStatus.Registering:
                case ActorStatus.ReadyToConnect:
                case ActorStatus.Connecting:
                    return true;

                case ActorStatus.Done:
                case ActorStatus.Stopped:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task RunAsync()
        {
            this.instance.InitRequired();

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now < this.whenToRun) return;

            this.log.Debug(this.status.ToString(), () => new { this.deviceId });

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
            }
        }

        public void HandleEvent(ActorEvents e)
        {
            switch (e)
            {
                case ActorEvents.Started:
                    if (this.loopSettings.SchedulableFetches <= 0)
                        return;
                    this.loopSettings.SchedulableFetches--;

                    this.actorLogger.ActorStarted();
                    this.ScheduleCredentialsSetup();
                    break;

                case ActorEvents.FetchFailed:
                    if (this.loopSettings.SchedulableFetches <= 0)
                        return;
                    this.loopSettings.SchedulableFetches--;

                    this.failedFetchCount++;
                    this.actorLogger.DeviceFetchFailed();
                    this.ScheduleFetch();
                    break;

                case ActorEvents.DeviceNotFound:
                    if (this.loopSettings.SchedulableRegistrations <= 0)
                        return;
                    this.loopSettings.SchedulableRegistrations--;

                    this.actorLogger.DeviceNotFound();
                    this.ScheduleRegistration();
                    break;

                case ActorEvents.DeviceRegistered:
                    this.actorLogger.DeviceRegistered();
                    this.ScheduleConnection();
                    break;

                case ActorEvents.RegistrationFailed:
                    if (this.loopSettings.SchedulableRegistrations <= 0)
                        return;
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
                    this.status = ActorStatus.Done;
                    break;

                case ActorEvents.Disconnected:
                    this.actorLogger.DeviceDisconnected();
                    // TODO: this works for the time being, but disconnection should not always lead to a deregistration
                    //       e.g. there are simulation scenarios where the device might just need to disconnect
                    this.ScheduleDeregistration();
                    break;

                case ActorEvents.DisconnectionFailed:
                    this.actorLogger.DeviceDisconnectionFailed();
                    this.ScheduleDisconnection();
                    break;

                case ActorEvents.DeviceDeregistered:
                    this.actorLogger.DeviceDeregistered();
                    this.status = ActorStatus.Deleted;
                    break;

                case ActorEvents.DeregisterationFailed:
                    this.actorLogger.DeviceDeregistrationFailed();
                    this.ScheduleDeregistration();
                    break;

                case ActorEvents.TelemetryClientBroken:
                    this.Client?.DisconnectAsync();
                    this.Client?.DisposeInternalClient();
                    this.Client = null;
                    this.ScheduleConnection();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e, null);
            }
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
                this.log.Warn("Error while stopping", e);
            }
        }

        public void Delete()
        {
            try
            {
                this.ScheduleDisconnection();
                this.actorLogger.DisconnectingDevice();
            }
            catch (Exception e)
            {
                this.log.Warn("Error while deleting", () => new { e });
            }
        }

        public void DisposeClient()
        {
            if (this.Client == null) return;

            this.Client.DisposeInternalClient();
            this.Client = null;
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
            var pauseMsec = this.simulationContext.RateLimiting.GetPauseForNextRegistryOperation();
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
            var pauseMsec = this.simulationContext.RateLimiting.GetPauseForNextRegistryOperation();
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
            var pauseMsec = this.simulationContext.RateLimiting.GetPauseForNextConnection();
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

        private void ScheduleDeregistration()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var pauseMsec = this.simulationContext.RateLimiting.GetPauseForNextRegistryOperation();
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
            this.whenToRun = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
