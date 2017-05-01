// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime
{
    /// <summary>Simple helper capturing uptime information</summary>
    public static class Uptime
    {
        /// <summary>When the service started</summary>
        public static DateTime Start { get; } = DateTime.UtcNow;

        /// <summary>How long the service has been running</summary>
        public static TimeSpan Duration => DateTime.UtcNow.Subtract(Start);
    }
}
