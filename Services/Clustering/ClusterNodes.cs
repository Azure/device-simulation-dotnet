// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering
{
    public interface IClusterNodes
    {
        string GetCurrentNodeId();
        Task KeepAliveNodeAsync();
    }

    public class ClusterNodes : IClusterNodes
    {
        // Generate a node id when the class is loaded. The value is shared across threads in the process.
        private static readonly string currentProcessNodeId = GenerateSharedNodeId();
        
        private readonly ILogger log;
        
        public ClusterNodes(
            ILogger logger)
        {
            this.log = logger;
        }
        
        public string GetCurrentNodeId()
        {
            return currentProcessNodeId;
        }

        public Task KeepAliveNodeAsync()
        {
            throw new NotImplementedException("Need the new storage code first");
        }

        // Generate a unique value used to identify the current instance
        private static string GenerateSharedNodeId()
        {
            // Example: 12a34b5678901c23de4c5f6ab78c9012.2018-01-12T01:15:00
            return Guid.NewGuid().ToString("N") + "." + DateTimeOffset.UtcNow.ToString("s");
        }
    }
}
