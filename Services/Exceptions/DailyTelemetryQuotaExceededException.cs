// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    // This exception is thrown when a device client reaches the daily quota
    public class DailyTelemetryQuotaExceededException : Exception
    {
        public DailyTelemetryQuotaExceededException(string message) : base(message)
        {
        }

        public DailyTelemetryQuotaExceededException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
