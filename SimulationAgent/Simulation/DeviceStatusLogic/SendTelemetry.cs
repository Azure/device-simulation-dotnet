// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

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

        public SendTelemetry(ILogger logger)
        {
            this.log = logger;
        }

        public void Setup(string deviceId, DeviceType deviceType)
        {
            this.deviceId = deviceId;
        }

        public void Run(object context)
        {
            this.SetupRequired();

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
            }
            catch (Exception e)
            {
                this.log.Error("SendTelemetry failed",
                    () => new { this.deviceId, e.Message, Error = e.GetType().FullName });
            }

            this.log.Debug("SendTelemetry complete", () => new { this.deviceId });
        }

        private void SetupRequired()
        {
            if (this.deviceId == null)
            {
                throw new Exception("Application error: Setup() must be invoked before Run().");
            }
        }
    }
}
