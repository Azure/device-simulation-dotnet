// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
                    MessageTimer = timer,
                    Interval = message.Interval
                };

                timer.Setup(this.Run, messageContext);

                this.timers.Add(timer);
            }
        }

        public void Start()
        {
            this.log.Info("Starting SendTelemetry", () => new { this.deviceId });
            foreach (var timer in this.timers)
            {
                timer.RunOnce(0);
            }
        }

        public void Stop()
        {
            this.log.Info("Stopping SendTelemetry", () => new { this.deviceId });
            foreach (var timer in this.timers)
            {
                timer.Cancel();
            }
        }

        public void Run(object context)
        {
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Each message context contains references to device
            // actor and the message timer, that we use to pause here
            var messageContext = (SendTelemetryContext) context;
            try
            {
                try
                {
                    this.RunInternalAsync(context).Wait();
                }
                finally
                {
                    var passed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                    messageContext?.MessageTimer?.RunOnce(messageContext.Interval.TotalMilliseconds - passed);
                }
            }
            catch (ObjectDisposedException e)
            {
                this.log.Debug("The simulation was stopped and some of the context is not available", () => new { e });
            }
        }

        private async Task RunInternalAsync(object context)
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

            // Send the telemetry message if the device is online
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
                    await actor.Client.SendMessageAsync(msg, message.MessageSchema);

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
                    () => new { this.deviceId, e });
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
