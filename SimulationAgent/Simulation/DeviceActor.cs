// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
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
        private List<ICancelable> tasks;

        public DeviceActor()
        {
            this.tasks = new List<ICancelable>();
            this.Receive<DeviceActorMessages.Setup>(msg => this.DoSetup(msg.Position, msg.DeviceType));
            this.Receive<DeviceActorMessages.Start>(msg => this.DoStart());
            this.Receive<DeviceActorMessages.Stop>(msg => this.DoStop());
            this.Receive<DeviceActorMessages.SendTelemetry>(msg => this.DoSendTelemetry(msg.Message));
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
                var telemetryMsg = new DeviceActorMessages.SendTelemetry(message);
                var task = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                    TimeSpan.Zero, message.Interval, this.Self, telemetryMsg, this.Self);
                this.tasks.Add(task);
            }
        }

        private void DoStop()
        {
            if (!this.isRunning) return;

            foreach (var task in this.tasks)
            {
                task.Cancel();
            }

            this.isRunning = false;
        }

        private void DoSendTelemetry(DeviceType.DeviceTypeMessage message)
        {
            Console.WriteLine("Sending telemetry: " + message.Message);
        }
    }
}
