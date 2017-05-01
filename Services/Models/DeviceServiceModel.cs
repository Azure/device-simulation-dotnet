// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class DeviceServiceModel
    {
        public string Etag { get; set; }
        public string Id { get; set; }
        public int C2DMessageCount { get; set; }
        public DateTime LastActivity { get; set; }
        public bool Connected { get; set; }
        public bool Enabled { get; set; }
        public DateTime LastStatusUpdated { get; set; }
        public DeviceTwinServiceModel Twin { get; set; }

        public DeviceServiceModel(
            string etag,
            string id,
            int c2DMessageCount,
            DateTime lastActivity,
            bool connected,
            bool enabled,
            DateTime lastStatusUpdated,
            DeviceTwinServiceModel twin)
        {
            this.Etag = etag;
            this.Id = id;
            this.C2DMessageCount = c2DMessageCount;
            this.LastActivity = lastActivity;
            this.Connected = connected;
            this.Enabled = enabled;
            this.LastStatusUpdated = lastStatusUpdated;
            this.Twin = twin;
        }

        public DeviceServiceModel(Device azureDevice, DeviceTwinServiceModel twin) :
            this(
                etag: azureDevice.ETag,
                id: azureDevice.Id,
                c2DMessageCount: azureDevice.CloudToDeviceMessageCount,
                lastActivity: azureDevice.LastActivityTime,
                connected: azureDevice.ConnectionState.Equals(DeviceConnectionState.Connected),
                enabled: azureDevice.Status.Equals(DeviceStatus.Enabled),
                lastStatusUpdated: azureDevice.StatusUpdatedTime,
                twin: twin)
        {
        }

        public DeviceServiceModel(Device azureDevice, Twin azureTwin) :
            this(azureDevice, new DeviceTwinServiceModel(azureTwin))
        {
        }

        public Device ToAzureModel()
        {
            return new Device(this.Id)
            {
                ETag = this.Etag
            };
        }
    }
}
