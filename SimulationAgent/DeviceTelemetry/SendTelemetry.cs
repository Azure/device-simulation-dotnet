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
            var states = this.context.DeviceState.GetAll();

            // device could be rebooting, updating firmware, etc.
            if (states.ContainsKey("online") && !(bool)states["online"])
            {
                this.log.Debug("No telemetry will be sent because the device is offline...", () => new { this.deviceId });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDelivered);
                return;
            }

            // Inject the device state into the message template
            var msg = this.message.MessageTemplate;
            foreach (var value in states)
            {
                msg = msg.Replace("${" + value.Key + "}", value.Value.ToString());
            }

            await this.SendTelemetryMessageAsync(msg);
        }

        private async Task SendTelemetryMessageAsync(string msg)
        {
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            try
            {
                // Used to count messages sent, could be moved to Run()
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.SendingTelemetry);

                await this.context.Client.SendMessageAsync(msg, this.message.MessageSchema);

                this.log.Debug("Telemetry delivered", () => new { timeSpentMsecs = GetTimeSpentMsecs(), this.deviceId, MessageSchema = this.message.MessageSchema.Name });

                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDelivered);
            }
            catch (DailyTelemetryQuotaExceededException e)
            {
                this.log.Warn("Client reached the daily quota", () => new { timeSpentMsecs = GetTimeSpentMsecs(), this.deviceId, e });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryQuotaExceeded);
            }
            catch (BrokenDeviceClientException e)
            {
                this.log.Error("Client broke while sending telemetry", () => new { timeSpentMsecs = GetTimeSpentMsecs(), this.deviceId, e });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryClientBroken);
            }
            catch (TelemetrySendTimeoutException e)
            {
                this.log.Error("Telemetry delivery timeout", () => new { timeSpentMsecs = GetTimeSpentMsecs(), this.deviceId, e });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendFailure);
            }
            catch (TelemetrySendException e)
            {
                this.log.Error("Unexpected telemetry error", () => new { timeSpentMsecs = GetTimeSpentMsecs(), this.deviceId, e });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendFailure);
            }
            catch (ResourceNotFoundException e)
            {
                this.log.Error("Telemetry error, the device is not registered", () => new { timeSpentMsecs = GetTimeSpentMsecs(), this.deviceId, e });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.DeviceNotFound);
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while sending telemetry", () => new { timeSpentMsecs = GetTimeSpentMsecs(), this.deviceId, e });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendFailure);
            }
        }
    }
}
