// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceReplay
{
    public interface IDeviceReplayActor
    {
        Task Init(ISimulationContext simulationContext,
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
        private readonly IReplayFileService replayFileService;

        private ActorStatus status;
        private string deviceId;
        private string currentLine;
        private string file;
        private StringReader fileReader;
        private long whenToRun;
        private long prevInterval;
        private IDeviceConnectionActor deviceContext;
        private DeviceModelMessageSchema emptySchema;

        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Replay file constants
        private const string TELEMETRY_TYPE = "telemetry";
        private const int NUM_CSV_COLS = 3;
        private const int MS_HOUR = 3600000;
        private const int MS_MINUTE = 60000;
        private const int MS_SECOND = 1000;

        public DeviceReplayActor(
            ILogger logger,
            IActorsLogger actorLogger,
            IServicesConfig config,
            IEngines engines,
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
            this.emptySchema = new DeviceModelMessageSchema();
            this.replayFileService = new ReplayFileService(config, engines, logger);
        }

        /// <summary>
        /// Invoke this method before calling Execute(), to initialize the actor
        /// with details like the device model and message type to simulate.
        /// </summary>
        public async Task Init(ISimulationContext simulationContext,
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

            string fileId = simulationContext.ReplayFileId;
            try
            {
                if (!string.IsNullOrEmpty(fileId))
                {
                    var data = await this.replayFileService.GetAsync(fileId);
                    this.file = data.Content;
                    this.fileReader = new StringReader(this.file);
                    this.status = ActorStatus.ReadLine;
                }
            }
            catch (Exception e)
            {
                this.log.Error("Failed to read line", () => new { this.deviceId, e });
            }

            this.instance.InitComplete();
        }

        public bool HasWorkToDo()
        {
            if (Now < this.whenToRun) return false;

            if (!this.deviceContext.Connected) return false;

            switch (this.status) 
            {
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
                    this.fileReader.Dispose();
                    this.fileReader = new StringReader(this.file);
                    this.status = ActorStatus.ReadLine;
                    break;
            }
        }

        public void Stop()
        {
            this.log.Debug("Device replay actor stopped",
                () => new { this.deviceId, Status = this.status.ToString() });

            // Discard file reader resources
            this.fileReader.Dispose();

            this.status = ActorStatus.Stopped;
        }

        private async void SendTelemetry() 
        {
            try
            {
                await this.deviceContext.Client.SendMessageAsync(this.currentLine, this.emptySchema);
                this.status = ActorStatus.ReadLine;
                this.log.Debug("Sending message", () => new { this.deviceId });
            }
            catch (Exception e)
            {
                this.Stop();
                this.log.Error("Failed to send message", () => new { this.deviceId, e });
            }
        }

        private void ReadLine() 
        {
            try
            {
                this.currentLine = this.fileReader.ReadLine();
                if (this.currentLine == null)
                {
                    if (this.simulationContext.ReplayFileIndefinitely)
                    {
                        this.status = ActorStatus.Restart;
                    }
                    else 
                    {
                        this.Stop();
                    }
                }
                else 
                {
                    // Check for incorrectly formed csv
                    var values = this.currentLine.Split(',');
                    if (values.Length >= NUM_CSV_COLS && values[0] == TELEMETRY_TYPE) // Only send telemetry
                    {
                        var intervals = values[1].Split(':');
                        var msInterval = (long.Parse(intervals[0]) * MS_HOUR)
                            + (long.Parse(intervals[1]) * MS_MINUTE)
                            + (long.Parse(intervals[2]) * MS_SECOND);
                        this.currentLine = String.Join("", values, NUM_CSV_COLS - 1, values.Length - NUM_CSV_COLS - 1);
                        this.whenToRun = Now + msInterval - this.prevInterval;
                        this.prevInterval = msInterval;
                        this.status = ActorStatus.LineReady;
                    }
                }
            }
            catch (Exception e)
            {
                this.Stop();
                this.log.Error("Failed to read line", () => new { this.deviceId, e });
            }
        }
    }
}
