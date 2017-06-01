// Copyright (c) Microsoft. All rights reserved.

using System;
using Akka.Actor;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

// TODO: add DI - @see http://getakka.net/docs/Dependency%20injection
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation
{
    public class DeviceActor : ReceiveActor
    {
        private int position;
        private DeviceType deviceType;
        private IoTHubProtocol protocol;
        private bool isRunning;
        private ICancelable scheduleCancel;

        public class Setup
        {
            public DeviceType DeviceType { get; }
            public int Position { get; }

            public Setup(int position, DeviceType deviceType)
            {
                this.Position = position;
                this.DeviceType = deviceType;
            }
        }

        public class Start
        {
        }

        public class Stop
        {
        }

        public class SendTelemetry
        {
            public DeviceType.DeviceTypeMessage Message { get; }

            public SendTelemetry(DeviceType.DeviceTypeMessage message)
            {
                this.Message = message;
            }
        }

        public DeviceActor()
        {
            this.Receive<Setup>(msg => this.DoSetup(msg.Position, msg.DeviceType));
            this.Receive<Start>(msg => this.DoStart());
            this.Receive<Stop>(msg => this.DoStop());
            this.Receive<SendTelemetry>(msg => this.DoSendTelemetry(msg.Message));
        }

        private void DoSetup(int position, DeviceType deviceType)
        {
            this.isRunning = false;
            this.position = position;
            this.deviceType = deviceType;
            this.protocol = deviceType.Protocol;

            Console.WriteLine($"Starting device type {this.deviceType.Name}");
        }

        private void DoStart()
        {
            if (this.isRunning) return;
            this.isRunning = true;

            // Schedule telemetry sender
            foreach (var message in this.deviceType.Telemetry.Messages)
            {
                var telemetryMsg = new SendTelemetry(message);
                this.scheduleCancel = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                    TimeSpan.Zero, message.Interval, this.Self, telemetryMsg, this.Self);
            }
        }

        private void DoStop()
        {
            if (!this.isRunning) return;
            this.isRunning = false;

            // Stop telemetry sender
            this.scheduleCancel.Cancel();
        }

        private void DoSendTelemetry(DeviceType.DeviceTypeMessage message)
        {
            Console.WriteLine("Sending telemetry: " + message.Message);
        }
    }
}
