// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

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

        // Add time the event was written as a telemetry property
        private const string OCCURRENCE_TIME_PROPERTY = "occurrenceUtcTime";
        private const string DEVICE_ID_PROPERTY = "deviceId";

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
            var state = this.context.DeviceState.GetAll();

            this.log.Debug("Checking to see if device is online", () => new { this.deviceId });
            if ((bool)state["online"] == false)
            {
                // device could be rebooting, updating firmware, etc.
                this.log.Debug("No telemetry will be sent as the device is offline...", () => new { this.deviceId });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDelivered);
                return;
            }

            // device could be rebooting, updating firmware, etc.
            this.log.Debug("The device state says the device is online", () => new { this.deviceId });
            this.log.Debug("Sending telemetry...", () => new { this.deviceId });

            // Inject the device state into the message template
            this.log.Debug("Preparing the message content using the device state", () => new { this.deviceId });
            var msg = this.message.MessageTemplate;
            foreach (var value in state)
            {
                msg = msg.Replace("${" + value.Key + "}", value.Value.ToString());
            }

            // Kirpas: Temporary fix as metadata is not correctly interpreted by streaming solutions such as ASA
            var format = $"{{\"{OCCURRENCE_TIME_PROPERTY}\":\"{DateTimeOffset.UtcNow.ToString()}\",\"{DEVICE_ID_PROPERTY}\":\"{this.deviceId}\",";
            msg = msg.Replace("{", "");
            msg = String.Concat(format, msg);

            this.SendTelemetryMessage(msg);
        }

        private void SendTelemetryMessage(string msg)
        {
            this.log.Debug("Calling SendMessageAsync...",
                () => new { this.deviceId, MessageSchema = this.message.MessageSchema.Name, msg });

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.SendingTelemetry);

                /**
                 * ContinueWith allows to easily manage the exceptions here, with the ability to change
                 * the code to synchronous or asynchronous, via TaskContinuationOptions.
                 *
                 * Once the code successfully handle all the scenarios, with good throughput and low CPU usage
                 * we should see if the async/await syntax performs similarly/better.
                 */
                this.context.Client
                    .SendMessageAsync(msg, this.message.MessageSchema)
                    .ContinueWith(t =>
                    {
                        var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;

                        if (t.IsCanceled)
                        {
                            this.log.Warn("The telemetry sending task has been cancelled", () => new { this.deviceId, t.Exception });
                        }
                        else if (t.IsFaulted)
                        {
                            var exception = t.Exception.InnerExceptions.FirstOrDefault();
                            this.log.Error(GetLogErrorMessage(exception), () => new { this.deviceId, exception });
                            this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendFailure);
                        }
                        else if (t.IsCompleted)
                        {
                            this.log.Debug("Telemetry delivered", () => new { this.deviceId, timeSpent, MessageSchema = this.message.MessageSchema.Name });
                            this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDelivered);
                        }
                    },
                        TaskContinuationOptions.ExecuteSynchronously);
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while sending telemetry", () => new { this.deviceId, e });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendFailure);
            }
        }

        private static string GetLogErrorMessage(Exception e)
        {
            switch (e)
            {
                case TelemetrySendTimeoutException _:
                    return "Telemetry send timeout error";

                case TelemetrySendIOException _:
                    return "Telemetry send I/O error";

                case TelemetrySendException _:
                    return "Telemetry send unknown error";
            }

            return e != null ? "Telemetry send unknown error" : string.Empty;
        }

        /*
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

                    this.SendTelemetryMessageAsync(msg).Wait(TimeSpan.FromMinutes(1));
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
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendUnknownFailure);
            }
        }

        private async Task SendTelemetryMessageAsync(string msg)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.SendingTelemetry);

            try
            {
                await this.context.Client.SendMessageAsync(msg, this.message.MessageSchema);

                var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;
                this.log.Debug("Telemetry delivered", () => new { this.deviceId, timeSpent, MessageSchema = this.message.MessageSchema.Name });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDelivered);
            }
            catch (TelemetrySendTimeoutException exception)
            {
                this.log.Error("Telemetry send timeout error", () => new { this.deviceId, exception });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendFailure);
            }
            catch (TelemetrySendIOException exception)
            {
                this.log.Error("Telemetry send I/O error", () => new { this.deviceId, exception });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendFailure);
            }
            catch (TelemetrySendException exception)
            {
                this.log.Error("Telemetry send unknown error", () => new { this.deviceId, exception });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendUnknownFailure);
            }
            catch (Exception exception)
            {
                this.log.Error("Unexpected error", () => new { this.deviceId, exception });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendUnknownFailure);
            }
        }
        */
    }
}
