// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when a device failed sending messages to IoTHub.
    /// </summary>
    public class TelemetrySendException : CustomException
    {
        public TelemetrySendException(string message) : base(message)
        {
        }

        public TelemetrySendException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
