// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime
{
    /// <summary>Helper capturing runtime information.</summary>
    public static class Uptime
    {
        /// <summary>When the service started</summary>
        public static DateTimeOffset Start { get; } = DateTimeOffset.UtcNow;

        /// <summary>How long the service has been running</summary>
        public static TimeSpan Duration => DateTimeOffset.UtcNow.Subtract(Start);

        /// <summary>A randomly generated ID used to identify the process in the logs</summary>
        public static string ProcessId { get; } = "WebService." + Guid.NewGuid();
    }
}
