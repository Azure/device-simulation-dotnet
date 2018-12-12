// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
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

        public void Init(IDeviceTelemetryActor context, string deviceId, DeviceModel deviceModel)
        {
            this.context = context;
            this.deviceId = deviceId;
            this.message = context.Message;
        }

        public async Task RunAsync()
        {
            var state = this.context.DeviceState.GetAll();

            // device could be rebooting, updating firmware, etc.
            this.log.Debug("Checking to see if device is online", () => new { this.deviceId });
            if (!state.ContainsKey("online") || (bool) state["online"])
            {
                this.log.Debug("The device state says the device is online, sending telemetry...", () => new { this.deviceId });
            }
            else
            {
                this.log.Debug("No telemetry will be sent because the device is offline...", () => new { this.deviceId });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDelivered);
                return;
            }

            // Inject the device state into the message template
            this.log.Debug("Preparing the message content using the device state", () => new { this.deviceId });
            var msg = this.message.MessageTemplate;
            foreach (var value in state)
            {
                msg = msg.Replace("${" + value.Key + "}", value.Value.ToString());
            }

            await this.SendTelemetryMessageAsync(msg);
        }

        private async Task SendTelemetryMessageAsync(string msg)
        {
            this.log.Debug("Calling SendMessageAsync...",
                () => new { this.deviceId, MessageSchema = this.message.MessageSchema.Name, msg });

            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            try
            {
                // Used to count messages sent, could be moved to Run()
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.SendingTelemetry);

                await this.context.Client.SendMessageAsync(msg, this.message.MessageSchema);

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Debug("Telemetry delivered",
                    () => new { timeSpentMsecs, this.deviceId, MessageSchema = this.message.MessageSchema.Name });

                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDelivered);
            }
            catch (DailyTelemetryQuotaExceededException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Warn("Client reached the daily quota",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryQuotaExceeded);
            }
            catch (BrokenDeviceClientException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Client broke while sending telemetry",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryClientBroken);
            }
            catch (TelemetrySendTimeoutException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Telemetry delivery timeout",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendFailure);
            }
            catch (TelemetrySendException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Unexpected telemetry error",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendFailure);
            }
            catch (ResourceNotFoundException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Telemetry error, the device is not registered",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.DeviceNotFound);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Unexpected error while sending telemetry",
                    () => new { timeSpentMsecs, this.deviceId, e });

                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendFailure);
            }
        }
    }
}
