// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceReplay
{
    public interface IDeviceReplayActor
    {
        void Init(
            ISimulationContext simulationContext,
            string deviceId,
            DeviceModel deviceModel,
            IDeviceConnectionActor context);

        bool HasWorkToDo();
        Task RunAsync();
        void Stop();
    }

    public class DeviceReplayActor : IDeviceReplayActor
    {
        private enum ActorStatus
        {
            None,
            ReadLine,
            LineReady,
            Stopped,
            FileEnd,
            Restart
        }

        private readonly ILogger log;
        private readonly IActorsLogger actorLogger;
        private ISimulationContext simulationContext;
        private DeviceModel deviceModel;
        private readonly IInstance instance;

        private ActorStatus status;
        private string deviceId;
        private string currentLine;
        private StreamReader streamReader;
        private FileStream fileStream;
        private long whenToRun;
        private long prevInterval;
        private IDeviceConnectionActor deviceContext;

        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public DeviceReplayActor(
            ILogger logger,
            IActorsLogger actorLogger,
            IInstance instance)
        {
            this.log = logger;
            this.actorLogger = actorLogger;
            this.instance = instance;

            this.status = ActorStatus.None;
            this.deviceModel = null;
            this.currentLine = "";
            this.whenToRun = 0;
            this.prevInterval = 0;
        }

        /// <summary>
        /// TODO: Fill in comment
        /// </summary>
        public void Init(
            ISimulationContext simulationContext, 
            string deviceId, 
            DeviceModel deviceModel,
            IDeviceConnectionActor context)
        {
            this.instance.InitOnce();

            this.simulationContext = simulationContext;
            this.deviceModel = deviceModel;
            this.deviceId = deviceId;
            this.deviceContext = context;
            this.actorLogger.Init(deviceId, "Replay");

            string path = deviceModel.ReplayFile;
            try
            {
                // TODO: Pull from data store
                if (File.Exists(path))
                {
                    this.fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    this.streamReader = new StreamReader(this.fileStream);
                    this.status = ActorStatus.ReadLine;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }

            this.instance.InitComplete();
        }

        public bool HasWorkToDo()
        {
            if (Now < this.whenToRun) return false;

            if (!this.deviceContext.Connected) return false;

            switch (this.status) {
                case ActorStatus.ReadLine:
                case ActorStatus.LineReady:
                case ActorStatus.Restart:
                    return true;
            }
            return false;
        }

        public async Task RunAsync()
        {
            switch (this.status)
            {
                case ActorStatus.ReadLine:
                    this.ReadLine();
                    break;
                case ActorStatus.LineReady:
                    this.SendTelemetry();
                    break;
                case ActorStatus.Restart:
                    // Rewind the stream to the beginning
                    this.fileStream.Position = 0;
                    this.streamReader.DiscardBufferedData();
                    this.status = ActorStatus.ReadLine;
                    break;
            }
        }

        public void Stop()
        {
            this.log.Debug("Device replay actor stopped",
                () => new { this.deviceId, Status = this.status.ToString() });

            // Discard file reader resources
            this.fileStream.Dispose();
            this.streamReader.Dispose();

            this.status = ActorStatus.Stopped;

            // TODO: Stop the sim in the db
        }

        private async void SendTelemetry() {
            try
            {
                var emptySchema = new DeviceModelMessageSchema();
                await this.deviceContext.Client.SendMessageAsync(this.currentLine, emptySchema);
                this.status = ActorStatus.ReadLine;
                Console.WriteLine("Sending " + this.deviceId + ": {0}", this.currentLine);
            }
            catch (Exception e)
            {
                this.Stop();
                Console.WriteLine("Failed to send message", e.ToString());
            }
        }

        private void ReadLine() {
            try
            {
                this.currentLine = this.streamReader.ReadLine();
                if (this.currentLine == null)
                {
                    // TODO: Use real variable from the simulation
                    var runIndefinitely = false; 
                    if (runIndefinitely)
                    {
                        this.status = ActorStatus.Restart;
                    }
                    else {
                        this.Stop();
                    }
                }
                else {
                    // Check for incorrectly formed csv
                    var values = this.currentLine.Split(',');
                    if (values.Length >= 3 && values[0] == "telemetry") // Only send telemetry
                    {
                        var intervals = values[1].Split(':');
                        var msInterval = (long.Parse(intervals[0]) * 3600000)
                            + (long.Parse(intervals[1]) * 60000)
                            + (long.Parse(intervals[2]) * 1000);
                        this.currentLine = String.Join("", values, 2, values.Length - 2);
                        this.whenToRun = Now + msInterval - this.prevInterval;
                        this.prevInterval = msInterval;
                        this.status = ActorStatus.LineReady;
                    }
                }
            }
            catch (Exception e)
            {
                this.Stop();
                Console.WriteLine("Failed to read line", e.ToString());
            }
        }
    }
}
