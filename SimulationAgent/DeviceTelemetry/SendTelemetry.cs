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

        public void Setup(IDeviceTelemetryActor context, string deviceId, DeviceModel deviceModel)
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
            if ((bool) state["online"] == false)
            {
                this.log.Debug("No telemetry will be sent as the device is offline...", () => new { this.deviceId });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDelivered);
                return;
            }

            this.log.Debug("The device state says the device is online", () => new { this.deviceId });
            this.log.Debug("Sending telemetry...", () => new { this.deviceId });

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

            try
            {
                // Used to count messages sent, could be moved to Run()
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.SendingTelemetry);

                await this.context.Client.SendMessageAsync(msg, this.message.MessageSchema);

                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Debug("Telemetry delivered",
                    () => new { timeSpentMsecs, this.deviceId, MessageSchema = this.message.MessageSchema.Name });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDelivered);
            }
            catch (BrokenDeviceClientException e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Client broke while sending telemetry", () => new { timeSpentMsecs, this.deviceId, e });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryClientBroken);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Unexpected error while sending telemetry", () => new { timeSpentMsecs, this.deviceId, e });
                this.context.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendFailure);
            }
        }
    }
}
