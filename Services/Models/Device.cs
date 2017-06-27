// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class Device
    {
        public string Etag { get; set; }
        public string Id { get; set; }
        public int C2DMessageCount { get; set; }
        public DateTimeOffset LastActivity { get; set; }
        public bool Connected { get; set; }
        public bool Enabled { get; set; }
        public DateTimeOffset LastStatusUpdated { get; set; }
        public string IoTHubHostName { get; set; }
        public string AuthPrimaryKey { get; set; }
    }
}
