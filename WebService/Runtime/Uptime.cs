// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime
{
    public static class Uptime
    {
        public static DateTime Start { get; } = DateTime.UtcNow;
        public static TimeSpan Duration => DateTime.UtcNow.Subtract(Start);
    }
}
