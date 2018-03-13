// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry
{
    /// <summary>
    /// Logic executed after Connect() succeeds, to send telemetry.
    /// </summary>
    public class SendTelemetry : IDeviceTelemetryLogic
    {
        private readonly ILogger log;

        private string deviceId;

        private IDeviceTelemetryActor context;
        private DeviceModel.DeviceModelMessage message;

        public SendTelemetry(
            ILogger logger)
        {
            this.log = logger;
        }

        public void Setup(IDeviceTelemetryActor context, string deviceId, DeviceModel deviceModel)
        {
            this.context = context;
            this.deviceId = deviceId;
            this.message = context.Message;
        }

        public void Run()
        {
            this.log.Debug("Sending telemetry...", () => new { this.deviceId });

            try
            {
                var state = this.context.DeviceState;
                this.log.Debug("Checking to see if device is online", () => new { this.deviceId });
                if ((bool) state["online"])
                {
                    // device could be rebooting, updating firmware, etc.
                    this.log.Debug("The device state says the device is online", () => new { this.deviceId });

                    // Inject the device state into the message template
                    this.log.Debug("Preparing the message content using the device state", () => new { this.deviceId });
                    var msg = this.message.MessageTemplate;
                    foreach (var value in state)
                    {
                        msg = msg.Replace("${" + value.Key + "}", value.Value.ToString());
                    }

                    this.log.Debug("Calling SendMessageAsync...",
                        () => new { this.deviceId, MessageSchema = this.message.MessageSchema.Name, msg });

                    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.SendingTelemetry);
                    this.context.Client.SendMessageAsync(msg, this.message.MessageSchema)
                        .ContinueWith(t =>
                        {
                            if (t.IsFaulted && t.Exception is AggregateException)
                            {
                                var exceptions = t.Exception.InnerExceptions;
                                foreach (var exception in exceptions)
                                {
                                    if (exception != null && exception is TelemetrySendException)
                                    {
                                        this.log.Debug("Telemetry deliver failed", () => new { this.deviceId, exception });
                                        this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDeliveryFailed);
                                    }
                                }
                            }
                            else if (t.IsCompleted)
                            {
                                var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                                this.log.Debug("Telemetry delivered", () => new { this.deviceId, timeSpent, MessageSchema = this.message.MessageSchema.Name });
                                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDelivered);
                            }
                        });
                    
                }
                else
                {
                    // device could be rebooting, updating firmware, etc.
                    this.log.Debug("No telemetry will be sent as the device is offline...", () => new { this.deviceId });
                    this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDelivered);
                }
            }
            catch (Exception e)
            {
                this.log.Error("Telemetry error", () => new { this.deviceId, e });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDeliveryFailed);
            }
        }
    }
}