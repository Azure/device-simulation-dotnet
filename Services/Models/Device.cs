// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class Device
    {
        public string ETag { get; set; }
        public string Id { get; set; }
        public int C2DMessageCount { get; set; }
        public DateTimeOffset LastActivity { get; set; }
        public bool Connected { get; set; }
        public bool Enabled { get; set; }
        public DateTimeOffset LastStatusUpdated { get; set; }
        public string IoTHubHostName { get; set; }
        public string AuthPrimaryKey { get; set; }

        public Device(
            string eTag,
            string id,
            int c2DMessageCount,
            DateTimeOffset lastActivity,
            bool connected,
            bool enabled,
            DateTimeOffset lastStatusUpdated,
            string primaryKey,
            string ioTHubHostName)
        {
            this.ETag = eTag;
            this.Id = id;
            this.C2DMessageCount = c2DMessageCount;
            this.LastActivity = lastActivity;
            this.Connected = connected;
            this.Enabled = enabled;
            this.LastStatusUpdated = lastStatusUpdated;
            this.IoTHubHostName = ioTHubHostName;
            this.AuthPrimaryKey = primaryKey;
        }

        public Device(Azure.Devices.Device azureDevice, string ioTHubHostName) :
            this(
                eTag: azureDevice.ETag,
                id: azureDevice.Id,
                c2DMessageCount: azureDevice.CloudToDeviceMessageCount,
                lastActivity: azureDevice.LastActivityTime,
                connected: azureDevice.ConnectionState.Equals(DeviceConnectionState.Connected),
                enabled: azureDevice.Status.Equals(DeviceStatus.Enabled),
                lastStatusUpdated: azureDevice.StatusUpdatedTime,
                ioTHubHostName: ioTHubHostName,
                primaryKey: azureDevice.Authentication.SymmetricKey.PrimaryKey)
        {
        }
    }
}
