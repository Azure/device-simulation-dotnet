// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class StatusModel
    {
        public string Message { get; set; }
        public DateTime CurrentTime { get; set; }
        public DateTime StartTime => Uptime.Start;
        public TimeSpan UpTime => Uptime.Duration;

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "Status;" + Version.Name },
            { "$uri", "/" + Version.Name + "/status" }
        };
    }
}
