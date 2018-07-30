// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
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
        private readonly IInstance instance;

        private string deviceId;
        private IDeviceTelemetryActor deviceContext;
        private DeviceModel.DeviceModelMessage message;

        public SendTelemetry(ILogger logger, IInstance instance)
        {
            this.log = logger;
            this.instance = instance;
        }

        public void Init(IDeviceTelemetryActor deviceContext, string deviceId, DeviceModel deviceModel)
        {
            this.instance.InitOnce();

            this.deviceContext = deviceContext;
            this.deviceId = deviceId;
            this.message = deviceContext.Message;

            this.instance.InitComplete();
        }

        public async Task RunAsync()
        {
            this.instance.InitRequired();

            var state = this.deviceContext.DeviceState.GetAll();

            // device could be rebooting, updating firmware, etc.
            this.log.Debug("Checking to see if device is online", () => new { this.deviceId });
            if ((bool) state["online"] == false)
            {
                this.log.Debug("No telemetry will be sent as the device is offline...", () => new { this.deviceId });
                this.deviceContext.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDelivered);
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
                this.deviceContext.HandleEvent(DeviceTelemetryActor.ActorEvents.SendingTelemetry);

                await this.deviceContext.Client.SendMessageAsync(msg, this.message.MessageSchema);

                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Debug("Telemetry delivered",
                    () => new { timeSpentMsecs, this.deviceId, MessageSchema = this.message.MessageSchema.Name });
                this.deviceContext.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryDelivered);
            }
            catch (BrokenDeviceClientException e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Client broke while sending telemetry", () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceContext.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetryClientBroken);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Unexpected error while sending telemetry", () => new { timeSpentMsecs, this.deviceId, e });
                this.deviceContext.HandleEvent(DeviceTelemetryActor.ActorEvents.TelemetrySendFailure);
            }
        }
    }
}
