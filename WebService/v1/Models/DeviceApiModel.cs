// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DeviceApiModel
    {
        public string Etag { get; set; }
        public string Id { get; set; }
        public int C2DMessageCount { get; set; }
        public DateTime LastActivity { get; set; }
        public bool Connected { get; set; }
        public bool Enabled { get; set; }
        public DateTime LastStatusUpdated { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "Device;" + Version.Name },
            { "$uri", "/" + Version.Name + "/devices/" + this.Id },
            { "$twin_uri", "/" + Version.Name + "/devices/" + this.Id + "/twin" }
        };

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DeviceTwinApiModel Twin { get; set; }

        public DeviceApiModel()
        {
        }

        public DeviceApiModel(DeviceServiceModel device)
        {
            this.Id = device.Id;
            this.Etag = device.Etag;
            this.C2DMessageCount = device.C2DMessageCount;
            this.LastActivity = device.LastActivity;
            this.Connected = device.Connected;
            this.Enabled = device.Enabled;
            this.LastStatusUpdated = device.LastStatusUpdated;
            this.Twin = new DeviceTwinApiModel(device.Id,device.Twin);
        }

        public DeviceServiceModel ToServiceModel()
        {
            return new DeviceServiceModel
            (
                etag: this.Etag,
                id: this.Id,
                c2DMessageCount: this.C2DMessageCount,
                lastActivity: this.LastActivity,
                connected: this.Connected,
                enabled: this.Enabled,
                lastStatusUpdated: this.LastStatusUpdated,
                twin: this.Twin?.ToServiceModel()
            );
        }
    }
}
