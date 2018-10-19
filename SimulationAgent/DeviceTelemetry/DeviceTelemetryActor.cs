﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry
{
    public interface IDeviceTelemetryActor
    {
        ISmartDictionary DeviceState { get; }
        IDeviceClient Client { get; }
        DeviceModel.DeviceModelMessage Message { get; }
        long TotalMessagesCount { get; }
        long FailedMessagesCount { get; }

        void Init(
            ISimulationContext simulationContext,
            string deviceId,
            DeviceModel deviceModel,
            DeviceModel.DeviceModelMessage message,
            IDeviceStateActor deviceStateActor,
            IDeviceConnectionActor context);

        Task RunAsync();
        void HandleEvent(DeviceTelemetryActor.ActorEvents e);
        void Stop();
    }

    /**
     * TODO: when the device exists already, check whether it is tagged
     */
    public class DeviceTelemetryActor : IDeviceTelemetryActor
    {
        private enum ActorStatus
        {
            None,
            ReadyToStart,
            ReadyToSend,
            Sending,
            Stopped
        }

        public enum ActorEvents
        {
            Started,
            SendingTelemetry,
            TelemetrySendFailure,
            TelemetryClientBroken,
            TelemetryDelivered
        }

        private readonly ILogger log;
        private readonly IActorsLogger actorLogger;
        private readonly IDeviceTelemetryLogic sendTelemetryLogic;
        private readonly IInstance instance;

        private ActorStatus status;
        private string deviceId;
        private ISimulationContext simulationContext;
        private DeviceModel deviceModel;
        private long whenToRun;
        private long totalMessagesCount;
        private long failedMessagesCount;

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
        /// The telemetry message managed by this actor
        /// </summary>
        public DeviceModel.DeviceModelMessage Message { get; private set; }

        /// <summary>
        /// State maintained by the state actor
        /// </summary>
        public ISmartDictionary DeviceState => this.deviceStateActor.DeviceState;

        /// <summary>
        /// Azure IoT Hub client created by the connection actor
        /// </summary>
        public IDeviceClient Client => this.deviceContext.Client;

        /// <summary>
        /// Total messages count created by the connection actor
        /// </summary>
        public long TotalMessagesCount => this.totalMessagesCount;

        /// <summary>
        /// Failed messages count created by the connection actor
        /// </summary>
        public long FailedMessagesCount => this.failedMessagesCount;

        public DeviceTelemetryActor(
            ILogger logger,
            IActorsLogger actorLogger,
            SendTelemetry sendTelemetryLogic,
            IInstance instance)
        {
            this.log = logger;
            this.actorLogger = actorLogger;
            this.sendTelemetryLogic = sendTelemetryLogic;
            this.instance = instance;

            this.Message = null;

            this.status = ActorStatus.None;
            this.deviceModel = null;
            this.deviceId = null;
            this.deviceStateActor = null;
            this.totalMessagesCount = 0;
            this.failedMessagesCount = 0;
        }

        /// <summary>
        /// Invoke this method before calling Execute(), to initialize the actor
        /// with details like the device model and message type to simulate.
        /// </summary>
        public void Init(
            ISimulationContext simulationContext,
            string deviceId,
            DeviceModel deviceModel,
            DeviceModel.DeviceModelMessage message,
            IDeviceStateActor deviceStateActor,
            IDeviceConnectionActor context)
        {
            this.instance.InitOnce();

            this.simulationContext = simulationContext;
            this.deviceModel = deviceModel;
            this.Message = message;
            this.deviceId = deviceId;
            this.deviceStateActor = deviceStateActor;
            this.deviceContext = context;

            this.sendTelemetryLogic.Init(this, this.deviceId, this.deviceModel);
            this.actorLogger.Init(deviceId, "Telemetry");

            this.status = ActorStatus.ReadyToStart;

            this.instance.InitComplete();
        }

        public void Stop()
        {
            this.status = ActorStatus.Stopped;
        }

        // Run the next step and return a description about what happened
        public async Task RunAsync()
        {
            this.instance.InitRequired();

            this.log.Debug(this.status.ToString(), () => new { this.deviceId });

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now < this.whenToRun) return;

            switch (this.status)
            {
                case ActorStatus.ReadyToStart:
                    if (!this.deviceContext.Connected) return;

                    this.whenToRun = 0;
                    this.HandleEvent(ActorEvents.Started);
                    break;

                case ActorStatus.ReadyToSend:
                    if (!this.deviceContext.Connected) return;

                    this.status = ActorStatus.Sending;
                    this.actorLogger.SendingTelemetry();
                    await this.sendTelemetryLogic.RunAsync();
                    break;
            }
        }

        public void HandleEvent(ActorEvents e)
        {
            switch (e)
            {
                case ActorEvents.Started:
                    this.actorLogger.ActorStarted();
                    this.ScheduleTelemetry();
                    break;

                case ActorEvents.SendingTelemetry:
                    this.totalMessagesCount++;
                    break;

                case ActorEvents.TelemetryDelivered:
                    this.actorLogger.TelemetryDelivered();
                    this.ScheduleTelemetry();
                    break;

                case ActorEvents.TelemetryClientBroken:
                    this.failedMessagesCount++;
                    this.actorLogger.TelemetryFailed();
                    this.deviceContext.HandleEvent(DeviceConnectionActor.ActorEvents.TelemetryClientBroken);
                    this.Reset();
                    break;

                case ActorEvents.TelemetrySendFailure:
                    this.failedMessagesCount++;
                    this.actorLogger.TelemetryFailed();
                    this.ScheduleTelemetryRetry();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e, null);
            }
        }

        private void Reset()
        {
            this.status = ActorStatus.ReadyToStart;
        }

        private void ScheduleTelemetry()
        {
            // considering the throttling settings, when can the message be sent
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var availableSchedule = now + this.simulationContext.RateLimiting.GetPauseForNextMessage();

            // looking at the simulation settings, when should the message be sent
            // note: this.whenToRun contains the time when the last msg was sent
            var optimalSchedule = this.whenToRun + (long) this.Message.Interval.TotalMilliseconds;

            this.whenToRun = Math.Max(optimalSchedule, availableSchedule);
            this.status = ActorStatus.ReadyToSend;

            this.actorLogger.TelemetryScheduled(this.whenToRun);
            this.log.Debug("Telemetry scheduled",
                () => new
                {
                    this.deviceId,
                    Status = this.status.ToString(),
                    When = this.log.FormatDate(this.whenToRun)
                });
        }

        private void ScheduleTelemetryRetry()
        {
            // TODO: Work in progress - CPU perf optimization

            // Ignore the retry, just schedule the next message
            this.ScheduleTelemetry();

            // TODO: back off? - this retry logic is probably overloading the CPU

            /*
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var pauseMsec = this.rateLimiting.GetPauseForNextMessage();
            this.whenToRun = now + pauseMsec;
            this.status = ActorStatus.ReadyToSend;

            this.actorLogger.TelemetryRetryScheduled(this.whenToRun);
            this.log.Debug("Telemetry retry scheduled",
                () => new
                {
                    this.deviceId,
                    Status = this.status.ToString(),
                    When = this.log.FormatDate(this.whenToRun)
                });
            */
        }
    }
}
