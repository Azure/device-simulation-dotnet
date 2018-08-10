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

        void CredentialsSetupScheduled(long time);
        void FetchScheduled(long time);
        void PreparingDeviceCredentials();
        void FetchingDevice();
        void DeviceCredentialsReady();
        void DeviceFetched();
        void DeviceNotFound();
        void DeviceFetchFailed();

        void RegistrationScheduled(long time);
        void RegisteringDevice();
        void DeviceRegistered();
        void DeviceRegistrationFailed();

        void DeviceTaggingScheduled(long time);
        void TaggingDevice();
        void DeviceTagged();
        void DeviceTaggingFailed();

        void DeviceConnectionScheduled(long time);
        void ConnectingDevice();
        void DeviceConnected();
        void DeviceConnectionAuthFailed();
        void DeviceConnectionFailed();

        void TelemetryScheduled(long time);
        void SendingTelemetry();
        void TelemetryDelivered();
        void TelemetryFailed();

        void DevicePropertiesUpdateScheduled(long time, bool isRetry);
        void UpdatingDeviceProperties();
        void DevicePropertiesUpdated();
        void DevicePropertiesUpdateFailed();
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
        private string propertiesLogFile;
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
            this.propertiesLogFile = this.path + Path.DirectorySeparatorChar + "properties.log";
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

        public void CredentialsSetupScheduled(long time)
        {
            if (!this.enabled) return;

            var msg = DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(DATE_FORMAT);
            this.Log("Credentials setup scheduled at: " + msg);
            this.LogRegistry("Credentials setup scheduled at: " + msg);
        }

        public void PreparingDeviceCredentials()
        {
            if (!this.enabled) return;

            this.Log("Preparing device credentials");
            this.LogRegistry("Preparing device credentials");
        }

        public void DeviceCredentialsReady()
        {
            if (!this.enabled) return;

            this.Log("Device credentials ready");
            this.LogRegistry("Device credentials ready");
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

        public void DeviceTaggingScheduled(long time)
        {
            if (!this.enabled) return;

            var msg = DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(DATE_FORMAT);
            this.Log("Device tagging scheduled at: " + msg);
            this.LogProperties("Device tagging scheduled at: " + msg);
        }

        public void TaggingDevice()
        {
            if (!this.enabled) return;

            this.Log("Tagging device");
            this.LogProperties("Tagging");
        }

        public void DeviceTagged()
        {
            if (!this.enabled) return;

            this.Log("Device tagged");
            this.LogProperties("Device tagged");
        }

        public void DeviceTaggingFailed()
        {
            if (!this.enabled) return;

            this.Log("Device tagging FAILED");
            this.LogProperties("Device tag FAILED");
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

        public void DeviceConnectionAuthFailed()
        {
            if (!this.enabled) return;

            this.Log("Device auth failed");
            this.LogConnection("Device auth failed");
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

        public void DevicePropertiesUpdateScheduled(long time, bool isRetry)
        {
            if (!this.enabled) return;

            var msg = DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(DATE_FORMAT);

            if (isRetry)
            {
                this.Log("Device properties update retry scheduled at: " + msg);
                this.LogProperties("Retry scheduled at: " + msg);
            }
            else
            {
                this.Log("Device properties update scheduled at: " + msg);
                this.LogProperties("Device properties update scheduled at: " + msg);
            }
        }

        public void UpdatingDeviceProperties()
        {
            if (!this.enabled) return;

            this.Log("Updating device properties");
            this.LogProperties("Updating");
        }

        public void DevicePropertiesUpdated()
        {
            if (!this.enabled) return;

            this.Log("Device properties updated");
            this.LogProperties("Updated");
        }

        public void DevicePropertiesUpdateFailed()
        {
            if (!this.enabled) return;

            this.Log("Device properties update FAILED");
            this.LogProperties("Device properties update FAILED");
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

        private void LogProperties(string msg)
        {
            if (!this.enabled) return;

            var now = DateTimeOffset.UtcNow.ToString(DATE_FORMAT);
            this.WriteToFile(
                this.propertiesLogFile,
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
