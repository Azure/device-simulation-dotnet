﻿// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when a device fails to send a messages due to a timeout, typically due to throttling.
    /// </summary>
    public class TelemetrySendTimeoutException : CustomException
    {
        public TelemetrySendTimeoutException(string message) : base(message)
        {
        }

        public TelemetrySendTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
