// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    public interface IActorsLogger
    {
        void Init(string deviceId, string actorName);
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
        void DeregistrationScheduled(long time);
        void DeregisteringDevice();
        void DeviceDeregistered();
        void DeviceDeregistrationFailed();
        void DeviceQuotaExceeded();

        void DeviceTaggingScheduled(long time);
        void TaggingDevice();
        void DeviceTagged();
        void DeviceTaggingFailed();

        void DeviceConnectionScheduled(long time);
        void ConnectingDevice();
        void DeviceConnected();

        void DeviceDisconnectionFailed();
        void DeviceDisconnectionScheduled(long time);
        void DisconnectingDevice();
        void DeviceDisconnected();

        void DeviceConnectionAuthFailed();
        void DeviceConnectionFailed();

        void TelemetryScheduled(long time);
        void SendingTelemetry();
        void TelemetryDelivered();
        void TelemetryFailed();
        void DailyTelemetryQuotaExceeded();
        void TelemetryPaused(long time);

        void DevicePropertiesUpdateScheduled(long time, bool isRetry);
        void UpdatingDeviceProperties();
        void DevicePropertiesUpdated();
        void DevicePropertiesUpdateFailed();
    }
}
