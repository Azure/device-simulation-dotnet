// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    /// <summary>
    /// Logic executed after Connect() succeeds, to send telemetry.
    /// </summary>
    public class SendTelemetry : IDeviceStatusLogic
    {
        // Device message delivery timeout
        private static readonly TimeSpan sendTimeout = TimeSpan.FromSeconds(5);

        private readonly ILogger log;
        private readonly DependencyResolution.IFactory factory;

        private string deviceId;

        // Each of this timers invoke Run() for a specific telemetry message
        // i.e. a device can send multiple messages, with different frequency
        private readonly List<ITimer> timers;

        // Ensure that setup is called once and only once (which helps also detecting thread safety issues)
        private bool setupDone = false;

        private IDeviceActor context;

        public SendTelemetry(
            DependencyResolution.IFactory factory,
            ILogger logger)
        {
            this.factory = factory;
            this.timers = new List<ITimer>();
            this.log = logger;
        }

        public void Setup(string deviceId, DeviceModel deviceModel, IDeviceActor context)
        {
            if (this.setupDone)
            {
                this.log.Error("Setup has already been invoked, are you sharing this instance with multiple devices?",
                    () => new { this.deviceId });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.setupDone = true;
            this.deviceId = deviceId;
            this.context = context;

            foreach (var message in deviceModel.Telemetry)
            {
                this.log.Debug("Preparing telemetry timer", () =>
                    new { this.deviceId, message.Interval.TotalSeconds, message.MessageSchema.Name, message.MessageTemplate });

                var timer = this.factory.Resolve<ITimer>();

                var messageContext = new SendTelemetryContext
                {
                    DeviceActor = context,
                    Message = message,
                    MessageTimer = timer
                };

                timer.Setup(this.Run, message.Interval, messageContext);

                this.timers.Add(timer);
            }
        }

        public void Start()
        {
            this.log.Info("Starting SendTelemetry timers",
                () => new { this.context.DeviceId });
            foreach (var timer in this.timers)
            {
                timer.Start();
            }
        }

        public void Stop()
        {
            this.log.Info("Stopping SendTelemetry timers",
                () => new { this.context.DeviceId });
            foreach (var timer in this.timers)
            {
                timer.Stop();
            }
        }

        public void Run(object context)
        {
            // Each message context contains references to device
            // actor and the message timer, that we use to pause here
            var messageContext = (SendTelemetryContext) context;

            try
            {
                messageContext.MessageTimer.Pause();
                this.RunInternal(context);
            }
            finally
            {
                messageContext.MessageTimer.Resume();
            }
        }

        private void RunInternal(object context)
        {
            this.ValidateSetup();

            var messageContext = (SendTelemetryContext) context;
            var actor = messageContext.DeviceActor;
            var message = messageContext.Message;

            if (actor.CancellationToken.IsCancellationRequested)
            {
                actor.Stop();
                return;
            }

            // Send the telemetry message
            try
            {
                this.log.Debug("Checking to see if device is online", () => new { this.deviceId });
                if ((bool) actor.DeviceState["online"])
                {
                    // Inject the device state into the message template
                    var msg = message.MessageTemplate;
                    lock (actor.DeviceState)
                    {
                        foreach (var value in actor.DeviceState)
                        {
                            msg = msg.Replace("${" + value.Key + "}", value.Value.ToString());
                        }
                    }

                    this.log.Debug("SendTelemetry...",
                        () => new { this.deviceId, MessageSchema = message.MessageSchema.Name, msg });
                    actor.Client
                        .SendMessageAsync(msg, message.MessageSchema)
                        .Wait(sendTimeout);

                    this.log.Debug("SendTelemetry complete", () => new { this.deviceId });
                }
                else
                {
                    // device could be rebooting, updating firmware, etc.
                    this.log.Debug("No telemetry will be sent as the device is not online...",
                        () => new { this.deviceId, actor.DeviceState });
                }
            }
            catch (Exception e)
            {
                this.log.Error("SendTelemetry failed",
                    () => new { this.deviceId, e.Message, Error = e.GetType().FullName });
            }
        }

        private void ValidateSetup()
        {
            if (!this.setupDone)
            {
                this.log.Error("Application error: Setup() must be invoked before Run().",
                    () => new { this.deviceId });
                throw new DeviceActorAlreadyInitializedException();
            }
        }
    }
}
