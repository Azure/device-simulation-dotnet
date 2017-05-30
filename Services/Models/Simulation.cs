// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class Simulation
    {
        public string Etag { get; set; }
        public string Id { get; set; }
        public bool Enabled { get; set; }
        public IList<DeviceTypeRef> DeviceTypes { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Modified { get; set; }
        public long Version { get; set; }

        public Simulation()
        {
            this.DeviceTypes = new List<DeviceTypeRef>();
        }

        public class DeviceTypeRef
        {
            public string Id { get; set; }
            public int Count { get; set; }
        }
    }
}
