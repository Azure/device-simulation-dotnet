// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    // Singleton shim used to save memory when extra diagnostics not used
    public class ActorsLoggerShim : IActorsLogger
    {
        public void Init(string deviceId, string actorName)
        {
        }

        public void ActorStarted()
        {
        }

        public void ActorStopped()
        {
        }

        public void CredentialsSetupScheduled(long time)
        {
        }

        public void FetchScheduled(long time)
        {
        }

        public void PreparingDeviceCredentials()
        {
        }

        public void FetchingDevice()
        {
        }

        public void DeviceCredentialsReady()
        {
        }

        public void DeviceFetched()
        {
        }

        public void DeviceNotFound()
        {
        }

        public void DeviceFetchFailed()
        {
        }

        public void RegistrationScheduled(long time)
        {
        }

        public void RegisteringDevice()
        {
        }

        public void DeviceRegistered()
        {
        }

        public void DeviceRegistrationFailed()
        {
        }

        public void DeregistrationScheduled(long time)
        {
        }

        public void DeregisteringDevice()
        {
        }

        public void DeviceDeregistered()
        {
        }

        public void DeviceDeregistrationFailed()
        {
        }

        public void DeviceQuotaExceeded()
        {
        }

        public void DeviceTaggingScheduled(long time)
        {
        }

        public void TaggingDevice()
        {
        }

        public void DeviceTagged()
        {
        }

        public void DeviceTaggingFailed()
        {
        }

        public void DeviceConnectionScheduled(long time)
        {
        }

        public void ConnectingDevice()
        {
        }

        public void DeviceConnected()
        {
        }

        public void DeviceDisconnectionFailed()
        {
        }

        public void DeviceDisconnectionScheduled(long time)
        {
        }

        public void DisconnectingDevice()
        {
        }

        public void DeviceDisconnected()
        {
        }

        public void DeviceConnectionAuthFailed()
        {
        }

        public void DeviceConnectionFailed()
        {
        }

        public void TelemetryScheduled(long time)
        {
        }

        public void SendingTelemetry()
        {
        }

        public void TelemetryDelivered()
        {
        }

        public void TelemetryFailed()
        {
        }

        public void DailyTelemetryQuotaExceeded()
        {
        }

        public void TelemetryPaused(long time)
        {
        }

        public void DevicePropertiesUpdateScheduled(long time, bool isRetry)
        {
        }

        public void UpdatingDeviceProperties()
        {
        }

        public void DevicePropertiesUpdated()
        {
        }

        public void DevicePropertiesUpdateFailed()
        {
        }
    }
}
