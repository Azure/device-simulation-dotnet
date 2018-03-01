﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry
{
    public interface IDeviceTelemetryActor
    {
        Dictionary<string, object> DeviceState { get; }
        IDeviceClient Client { get; }
        DeviceModel.DeviceModelMessage Message { get; }
        int TotalMessagesCount { get; }
        int FailedMessagesCount { get; }

        void Setup(
            string deviceId,
            DeviceModel deviceModel,
            DeviceModel.DeviceModelMessage message,
            IDeviceStateActor deviceStateActor,
            IDeviceConnectionActor deviceConnectionActor);

        string Run();
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
            TelemetryDeliveryFailed,
            TelemetryDelivered,
        }

        private readonly ILogger log;
        private readonly IActorsLogger actorLogger;
        private readonly IRateLimiting rateLimiting;
        private readonly IDeviceTelemetryLogic sendTelemetryLogic;

        private ActorStatus status;
        private string deviceId;
        private DeviceModel deviceModel;
        private long whenToRun;
        private int totalMessageCount = 0;
        private int failedMessageCount = 0;

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
        /// The telemetry message managed by this actor
        /// </summary>
        public DeviceModel.DeviceModelMessage Message { get; private set; }

        /// <summary>
        /// State maintained by the state actor
        /// </summary>
        public Dictionary<string, object> DeviceState => this.deviceStateActor.DeviceState;

        /// <summary>
        /// Azure IoT Hub client created by the connection actor
        /// </summary>
        public IDeviceClient Client => this.deviceConnectionActor.Client;

        /// <summary>
        /// Total messages count created by the connection actor
        /// </summary>
        public int TotalMessagesCount => this.totalMessageCount;

        /// <summary>
        /// Failed messages count created by the connection actor
        /// </summary>
        public int FailedMessagesCount => this.failedMessageCount;

        public DeviceTelemetryActor(
            ILogger logger,
            IActorsLogger actorLogger,
            IRateLimiting rateLimiting,
            SendTelemetry sendTelemetryLogic)
        {
            this.log = logger;
            this.actorLogger = actorLogger;
            this.rateLimiting = rateLimiting;
            this.sendTelemetryLogic = sendTelemetryLogic;

            this.Message = null;

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
        public void Setup(
            string deviceId,
            DeviceModel deviceModel,
            DeviceModel.DeviceModelMessage message,
            IDeviceStateActor deviceStateActor,
            IDeviceConnectionActor deviceConnectionActor)
        {
            if (this.status != ActorStatus.None)
            {
                this.log.Error("The actor is already initialized",
                    () => new { CurrentDeviceId = this.deviceId, NewDeviceModelName = deviceModel.Name });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.deviceModel = deviceModel;
            this.Message = message;
            this.deviceId = deviceId;
            this.deviceStateActor = deviceStateActor;
            this.deviceConnectionActor = deviceConnectionActor;

            this.sendTelemetryLogic.Setup(this, this.deviceId, this.deviceModel);
            this.actorLogger.Setup(deviceId, "Telemetry");

            this.status = ActorStatus.ReadyToStart;
        }

        public void Stop()
        {
            this.status = ActorStatus.Stopped;
        }

        public void HandleEvent(ActorEvents e)
        {
            switch (e)
            {
                case ActorEvents.Started:
                    this.actorLogger.ActorStarted();
                    this.ScheduleTelemetry();
                    break;
                case ActorEvents.TelemetryDelivered:
                    this.totalMessageCount++;
                    this.actorLogger.TelemetryDelivered();
                    this.ScheduleTelemetry();
                    break;
                case ActorEvents.TelemetryDeliveryFailed:
                    this.totalMessageCount++;
                    this.failedMessageCount++;
                    this.actorLogger.TelemetryFailed();
                    this.ScheduleTelemetryRetry();
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

                case ActorStatus.ReadyToSend:
                    this.status = ActorStatus.Sending;
                    this.actorLogger.SendingTelemetry();
                    this.sendTelemetryLogic.Run();
                    return "sent telemetry";
            }

            return null;
        }

        private void ScheduleTelemetry()
        {
            // considering the throttling settings, when can the message be sent
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var availableSchedule = now + this.rateLimiting.GetPauseForNextMessage();

            // looking at the simulation settings, when should the message be sent
            // note: this.whenToExecute contains the time when the last msg was sent
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
        }
    }
}
