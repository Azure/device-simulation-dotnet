// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DeviceTwinApiModel
    {
        public string Etag { get; set; }
        public string DeviceId { get; set; }
        public Dictionary<string, JToken> ReportedProperties { get; set; }
        public Dictionary<string, JToken> DesiredProperties { get; set; }
        public Dictionary<string, JToken> Tags { get; set; }
        public bool IsSimulated { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceTwin;" + Version.Name },
            { "$uri", "/" + Version.Name + "/devices/" + this.DeviceId + "/twin" }
        };

        public DeviceTwinApiModel(string deviceId, DeviceTwinServiceModel deviceTwin)
        {
            if (deviceTwin != null)
            {
                this.Etag = deviceTwin.Etag;
                this.DeviceId = deviceId;
                this.DesiredProperties = deviceTwin.DesiredProperties;
                this.ReportedProperties = deviceTwin.ReportedProperties;
                this.Tags = deviceTwin.Tags;
                this.IsSimulated = deviceTwin.IsSimulated;
            }
        }

        public DeviceTwinServiceModel ToServiceModel()
        {
            return new DeviceTwinServiceModel
            (
                etag: this.Etag,
                deviceId: this.DeviceId,
                desiredProperties: this.DesiredProperties,
                reportedProperties: this.ReportedProperties,
                tags: this.Tags,
                isSimulated: this.IsSimulated
            );
        }
    }
}
