// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class DevicesPartition
    {
        public string Id { get; set; }
        
        public string SimulationId { get; set; }
        
        public int Size { get; set; }
        
        public List<string> DeviceIds { get; set; }
    }
}
