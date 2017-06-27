// Copyright (c) Microsoft. All rights reserved.

/*
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
        private readonly ILogger log;
        private readonly IDevices devices;

        private readonly IDeviceClients clients;
        //private readonly List<ICancelable> tasks;

        private DeviceType deviceType;
        private IoTHubProtocol protocol;
        private string deviceId;
        private bool isReady;
        private bool isRunning;
        private List<ICancelable> tasks;
        private bool isBusy;

        public DeviceActor()
        public DeviceActor(
            ILogger logger,
            IDevices devices,
            IDeviceClients clients)
        {
            this.tasks = new List<ICancelable>();
            this.Receive<DeviceActorMessages.Setup>(msg => this.DoSetup(msg.Position, msg.DeviceType));
            this.log = logger;
            this.devices = devices;
            this.clients = clients;
            this.isReady = false;
            this.isRunning = false;
            this.isBusy = false;

            //this.tasks = new List<ICancelable>();
            this.Receive<DeviceActorMessages.Setup>(msg => this.DoSetup(msg.Position, msg.DeviceType));
            this.Receive<DeviceActorMessages.Start>(msg => this.DoStart());
            this.Receive<DeviceActorMessages.Stop>(msg => this.DoStop());
            this.Receive<DeviceActorMessages.SendTelemetry>(async msg => await this.DoSendTelemetry(msg.Message));
        }

        private void DoSetup(int position, DeviceType deviceType)
        {
            this.log.Debug("Setup...", () => { });
            this.isRunning = false;
            this.position = position;
            this.deviceType = deviceType;
            this.protocol = deviceType.Protocol;

            this.deviceId = "Simulated." + deviceType.Name + "." + position;
            try
            {
                var device = this.devices.GetOrCreateAsync(this.deviceId).Result;
                this.clients.Add(this.deviceId, this.devices.GetClient(device, deviceType.Protocol));
                this.isReady = true;
            }
            catch (Exception e)
            {
                this.log.Error("Unable to get the device details",
                    () => new { this.deviceId, Exception = e.GetType().FullName, e.Message });
                var repeatMessage = new DeviceActorMessages.Setup(position, deviceType);
                //TODO Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(10), this.Self, repeatMessage, this.Self);
            }

            this.log.Debug("Setup complete", () => { });
            Console.WriteLine($"Starting device type {this.deviceType.Name}");
        }

        private void DoStart()
        {
            this.log.Debug("Start...", () => { });
            if (this.isRunning) return;
            this.isRunning = true;

            // Schedule telemetry sender
            foreach (var message in this.deviceType.Telemetry.Messages)
            {
                var telemetryMsg = new DeviceActorMessages.SendTelemetry(message);
                //                var task = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                //                    TimeSpan.Zero, message.Interval, this.Self, telemetryMsg, this.Self);

                Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(5), this.Self, telemetryMsg, this.Self);

                //                this.tasks.Add(task);
            }

            this.log.Debug("Start complete", () => { });
        }

        private void DoStop()
        {
            this.log.Debug("Stop...", () => { });
            if (!this.isRunning) return;

            //            foreach (var task in this.tasks)
            //            {
            //                task.Cancel();
            //            }

            this.isRunning = false;

            this.log.Debug("Stop complete", () => { });
        }

        private async Task DoSendTelemetry(DeviceType.DeviceTypeMessage message)
        {
            Console.WriteLine("Sending telemetry: " + message.Message);
            this.log.Debug("SendTelemetry...", () => { });
            if (!this.isRunning) return;
            if (!this.isReady) return;
            if (this.isBusy)
            {
                this.log.Error("Skipping message, client is busy", () => new { message.Message });
                return;
            }

            this.isBusy = true;
            this.log.Debug("Sending telemetry", () => new { message.Message });
            await this.clients.Get(this.deviceId).SendMessageAsync(message);
            this.isBusy = false;

            // Schedule next
            var telemetryMsg = new DeviceActorMessages.SendTelemetry(message);
            Context.System.Scheduler.ScheduleTellOnce(message.Interval, this.Self, telemetryMsg, this.Self);

            this.log.Debug("SendTelemetry complete", () => { });
        }
    }
}
*/
