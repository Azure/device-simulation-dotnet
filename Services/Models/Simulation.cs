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
        public IList<DeviceModelRef> DeviceModels { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Modified { get; set; }
        public long Version { get; set; }

        public Simulation()
        {
            this.DeviceModels = new List<DeviceModelRef>();
        }

        public class DeviceModelRef
        {
            public string Id { get; set; }
            public int Count { get; set; }
        }
    }
}
