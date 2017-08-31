// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation.DeviceStatusLogic
{
    /// <summary>
    /// Logic executed after Connect() succeeds, to send telemetry.
    /// </summary>
    public class SendTelemetry : IDeviceStatusLogic
    {
        // When connecting or sending a message, timeout after 5 seconds
        private static readonly TimeSpan connectionTimeout = TimeSpan.FromSeconds(5);

        private readonly ILogger log;
        private string deviceId;

        // Ensure that setup is called once and only once (which helps also detecting thread safety issues)
        private bool setupDone = false;

        public SendTelemetry(ILogger logger)
        {
            this.log = logger;
        }

        public void Setup(string deviceId, DeviceModel deviceModel)
        {
            if (this.setupDone)
            {
                this.log.Error("Setup has already been invoked, are you sharing this instance with multiple devices?",
                    () => new { this.deviceId });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.setupDone = true;
            this.deviceId = deviceId;
        }

        public void Run(object context)
        {
            this.ValidateSetup();

            var callContext = (SendTelemetryContext) context;
            var actor = callContext.Self;
            var message = callContext.Message;

            if (actor.CancellationToken.IsCancellationRequested)
            {
                actor.Stop();
                return;
            }

            // Send the telemetry message
            try
            {
                if (actor.DeviceState["online"].ToString() == "True")
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
                        .Wait(connectionTimeout);
                    
                } else
                    // device could be rebooting, updating firmware, etc.
                    this.log.Debug("The device is not online, no will be sent telemetry will be sent...",
                            () => new { this.deviceId, actor.DeviceState });
            }
            catch (Exception e)
            {
                this.log.Error("SendTelemetry failed",
                    () => new { this.deviceId, e.Message, Error = e.GetType().FullName });
            }

            this.log.Debug("SendTelemetry complete", () => new { this.deviceId });
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
