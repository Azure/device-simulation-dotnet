using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;

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
            ReadingFile,
            Stopped
        }

        private readonly ILogger log;
        private readonly IActorsLogger actorLogger;
        private ISimulationContext simulationContext;
        private DeviceModel deviceModel;
        private readonly IInstance instance;

        private ActorStatus status;
        private string deviceId;
        private string currentLine;
        private StreamReader fileStream;
        private IDeviceConnectionActor deviceContext;

        public DeviceReplayActor(
            ILogger logger,
            IActorsLogger actorLogger,
            SendTelemetry sendTelemetryLogic,
            IInstance instance)
        {
            this.log = logger;
            this.actorLogger = actorLogger;
            this.instance = instance;

            this.status = ActorStatus.None;
            this.deviceModel = null;
            this.currentLine = "";
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
            // this.sendTelemetryLogic.Init(this, this.deviceId, this.deviceModel);
            this.actorLogger.Init(deviceId, "Replay");

            string path = deviceModel.ReplayFile;
            try
            {
                // TODO: Pull from data store
                if (File.Exists(path))
                {
                    this.fileStream = new StreamReader(path);
                    this.status = ActorStatus.ReadingFile;
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
            if (!this.deviceContext.Connected) return false;

            switch (this.status) {
                case ActorStatus.ReadingFile:
                    return true;
            }
            return false;
        }

        public async Task RunAsync()
        {
            switch (this.status)
            {
                case ActorStatus.ReadingFile:
                    this.readLine();
                    try
                    {
                        var line = "{\"temperature\": 51.32, \"temperature_unit\": \"fahrenheit\", \"humidity\": 69.59, \"humidity_unit\":\"RH\", \"pressure\": 440.20, \"pressure_unit\": \"psi\"}";
                        await this.deviceContext.Client.SendMessageAsync(line, this.deviceModel.Telemetry[0].MessageSchema);
                    }
                    catch (Exception e) {
                        Console.WriteLine("Failed to send message", e.ToString());
                    }
                    break;
            }
        }

        public void Stop()
        {
            this.log.Debug("Device replay actor stopped",
                () => new { this.deviceId, Status = this.status.ToString() });

            this.status = ActorStatus.Stopped;
        }

        private void readLine() {
            try
            {
                this.currentLine = this.fileStream.ReadLine();
                if (this.currentLine == null)
                {
                    this.Stop();
                }
                else {
                    // Check for incorrectly formed csv
                    var values = this.currentLine.Split(',');
                    this.currentLine = String.Join("", values, 2, values.Length - 2);
                    Console.WriteLine(this.deviceId + ": {0}", this.currentLine);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to read line");
            }
        }
    }
}
