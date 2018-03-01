// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    public interface IActorsLogger
    {
        void Setup(string deviceId, string actorName);
        void ActorStarted();
        void ActorStopped();

        void FetchScheduled(long time);
        void FetchingDevice();
        void DeviceFetched();
        void DeviceNotFound();
        void DeviceFetchFailed();

        void RegistrationScheduled(long time);
        void RegisteringDevice();
        void DeviceRegistered();
        void DeviceRegistrationFailed();

        void DeviceTwinTaggingScheduled(long time);
        void TaggingDeviceTwin();
        void DeviceTwinTagged();
        void DeviceTwinTaggingFailed();

        void DeviceConnectionScheduled(long time);
        void ConnectingDevice();
        void DeviceConnected();
        void DeviceConnectionFailed();

        void TelemetryScheduled(long time);
        void TelemetryRetryScheduled(long time);
        void SendingTelemetry();
        void TelemetryDelivered();
        void TelemetryFailed();

        void DeviceTwinUpdateScheduled(long time);
        void DeviceTwinUpdateRetryScheduled(long time);
        void UpdatingDeviceTwin();
        void DeviceTwinUpdated();
        void DeviceTwinUpdateFailed();
    }

    public class ActorsLogger : IActorsLogger
    {
        private const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss.fff";

        private readonly ILogger log;
        private readonly string path;
        private readonly bool enabledInConfig;
        private bool enabled;
        private string deviceId;
        private string actorName;
        private string actorLogFile;
        private string deviceLogFile;
        private string registryLogFile;
        private string twinLogFile;
        private string connectionsLogFile;
        private string telemetryLogFile;

        public ActorsLogger(ILoggingConfig config, ILogger logger)
        {
            this.enabled = false;
            this.enabledInConfig = config.ExtraDiagnostics;
            this.path = config.ExtraDiagnosticsPath.Trim();
            this.log = logger;
        }

        public void Setup(string deviceId, string actorName)
        {
            this.deviceId = deviceId;
            this.actorName = actorName;

            this.deviceLogFile = this.path + Path.DirectorySeparatorChar + "_." + this.deviceId + ".log";
            this.actorLogFile = this.path + Path.DirectorySeparatorChar + "actors." + this.actorName + ".log";
            this.registryLogFile = this.path + Path.DirectorySeparatorChar + "registry.log";
            this.twinLogFile = this.path + Path.DirectorySeparatorChar + "twins.log";
            this.connectionsLogFile = this.path + Path.DirectorySeparatorChar + "connections.log";
            this.telemetryLogFile = this.path + Path.DirectorySeparatorChar + "telemetry.log";

            this.enabled = this.enabledInConfig && !string.IsNullOrEmpty(this.path);
            
            if (!this.enabled) return;

            try
            {
                Directory.CreateDirectory(this.path);
                this.Log("Actor configured");
            }
            catch (Exception e)
            {
                this.log.Error("Unable to write to " + this.path, () => new { e });
                this.enabled = false;
            }
        }

        public void ActorStarted()
        {
            if (!this.enabled) return;

            this.Log("Actor started");
        }

        public void ActorStopped()
        {
            if (!this.enabled) return;

            this.Log("Actor stopped");
        }

        public void FetchScheduled(long time)
        {
            if (!this.enabled) return;

            var msg = DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(DATE_FORMAT);
            this.Log("Fetch scheduled at: " + msg);
            this.LogRegistry("Fetch scheduled at: " + msg);
        }

        public void FetchingDevice()
        {
            if (!this.enabled) return;

            this.Log("Fetching device");
            this.LogRegistry("Fetching device");
        }

        public void DeviceFetched()
        {
            if (!this.enabled) return;

            this.Log("Device fetched");
            this.LogRegistry("Fetched");
        }

        public void DeviceNotFound()
        {
            if (!this.enabled) return;

            this.Log("Device not found");
            this.LogRegistry("Not found");
        }

        public void DeviceFetchFailed()
        {
            if (!this.enabled) return;

            this.Log("Device fetch FAILED");
            this.LogRegistry("Fetch FAILED");
        }

        public void RegistrationScheduled(long time)
        {
            if (!this.enabled) return;

            var msg = DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(DATE_FORMAT);
            this.Log("Device registration scheduled at: " + msg);
            this.LogRegistry("Registration scheduled at: " + msg);
        }

        public void RegisteringDevice()
        {
            if (!this.enabled) return;

            this.Log("Registering device");
            this.LogRegistry("Registering");
        }

        public void DeviceRegistered()
        {
            if (!this.enabled) return;

            this.Log("Device registered");
            this.LogRegistry("Registered");
        }

        public void DeviceRegistrationFailed()
        {
            if (!this.enabled) return;

            this.Log("Device registration FAILED");
            this.LogRegistry("Registration FAILED");
        }

        public void DeviceTwinTaggingScheduled(long time)
        {
            if (!this.enabled) return;

            var msg = DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(DATE_FORMAT);
            this.Log("Twin tagging scheduled at: " + msg);
            this.LogTwin("Twin tagging scheduled at: " + msg);
        }

        public void TaggingDeviceTwin()
        {
            if (!this.enabled) return;

            this.Log("Tagging twin");
            this.LogTwin("Tagging");
        }

        public void DeviceTwinTagged()
        {
            if (!this.enabled) return;

            this.Log("Twin tagged");
            this.LogTwin("Twin tagged");
        }

        public void DeviceTwinTaggingFailed()
        {
            if (!this.enabled) return;

            this.Log("Twin tagging FAILED");
            this.LogTwin("Twin tag FAILED");
        }

        public void DeviceConnectionScheduled(long time)
        {
            if (!this.enabled) return;

            var msg = DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(DATE_FORMAT);
            this.Log("Device connection scheduled at: " + msg);
            this.LogConnection("Connection scheduled at: " + msg);
        }

        public void ConnectingDevice()
        {
            if (!this.enabled) return;

            this.Log("Connecting device");
            this.LogConnection("Connecting");
        }

        public void DeviceConnected()
        {
            if (!this.enabled) return;

            this.Log("Device connected");
            this.LogConnection("Connected");
        }

        public void DeviceConnectionFailed()
        {
            if (!this.enabled) return;

            this.Log("Connection FAILED");
            this.LogConnection("FAILED");
        }

        public void TelemetryScheduled(long time)
        {
            if (!this.enabled) return;

            var msg = DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(DATE_FORMAT);
            this.Log("Telemetry scheduled at: " + msg);
            this.LogTelemetry("Scheduled at: " + msg);
        }

        public void TelemetryRetryScheduled(long time)
        {
            if (!this.enabled) return;

            var msg = DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(DATE_FORMAT);
            this.Log("Telemetry retry scheduled at: " + msg);
            this.LogTelemetry("Retry scheduled at: " + msg);
        }

        public void SendingTelemetry()
        {
            if (!this.enabled) return;

            this.Log("Sending telemetry");
            this.LogTelemetry("Sending telemetry");
        }

        public void TelemetryDelivered()
        {
            if (!this.enabled) return;

            this.Log("Telemetry delivered");
            this.LogTelemetry("Delivered");
        }

        public void TelemetryFailed()
        {
            if (!this.enabled) return;

            this.Log("Telemetry FAILED");
            this.LogTelemetry("FAILED");
        }

        public void DeviceTwinUpdateScheduled(long time)
        {
            if (!this.enabled) return;

            var msg = DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(DATE_FORMAT);
            this.Log("Twin update scheduled at: " + msg);
            this.LogTwin("Twin update scheduled at: " + msg);
        }

        public void DeviceTwinUpdateRetryScheduled(long time)
        {
            if (!this.enabled) return;

            var msg = DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(DATE_FORMAT);
            this.Log("Twin update retry scheduled at: " + msg);
            this.LogTwin("Retry scheduled at: " + msg);
        }

        public void UpdatingDeviceTwin()
        {
            if (!this.enabled) return;

            this.Log("Updating twin");
            this.LogTwin("Updating");
        }

        public void DeviceTwinUpdated()
        {
            if (!this.enabled) return;

            this.Log("Twin updated");
            this.LogTwin("Updated");
        }

        public void DeviceTwinUpdateFailed()
        {
            if (!this.enabled) return;

            this.Log("Twin update FAILED");
            this.LogTwin("Twin update FAILED");
        }

        private void Log(string msg)
        {
            if (!this.enabled) return;

            this.DeviceLog(msg);
            this.ActorLog(msg);
        }

        private void ActorLog(string msg)
        {
            if (!this.enabled) return;

            var now = DateTimeOffset.UtcNow.ToString(DATE_FORMAT);
            this.WriteToFile(
                this.actorLogFile,
                $"{now} - {this.deviceId} - {msg}\n");
        }

        private void DeviceLog(string msg)
        {
            if (!this.enabled) return;

            var now = DateTimeOffset.UtcNow.ToString(DATE_FORMAT);
            this.WriteToFile(
                this.deviceLogFile,
                $"{now} - {this.actorName} - {msg}\n");
        }

        private void LogRegistry(string msg)
        {
            if (!this.enabled) return;

            var now = DateTimeOffset.UtcNow.ToString(DATE_FORMAT);
            this.WriteToFile(
                this.registryLogFile,
                $"{now} - {this.deviceId} - {msg}\n");
        }

        private void LogTwin(string msg)
        {
            if (!this.enabled) return;

            var now = DateTimeOffset.UtcNow.ToString(DATE_FORMAT);
            this.WriteToFile(
                this.twinLogFile,
                $"{now} - {this.deviceId} - {msg}\n");
        }

        private void LogConnection(string msg)
        {
            if (!this.enabled) return;

            var now = DateTimeOffset.UtcNow.ToString(DATE_FORMAT);
            this.WriteToFile(
                this.connectionsLogFile,
                $"{now} - {this.deviceId} - {msg}\n");
        }

        private void LogTelemetry(string msg)
        {
            if (!this.enabled) return;

            var now = DateTimeOffset.UtcNow.ToString(DATE_FORMAT);
            this.WriteToFile(
                this.telemetryLogFile,
                $"{now} - {this.deviceId} - {msg}\n");
        }

        private void WriteToFile(string filename, string text)
        {
            if (!this.enabled) return;

            this.log.LogToFile(filename, text);
        }
    }
}