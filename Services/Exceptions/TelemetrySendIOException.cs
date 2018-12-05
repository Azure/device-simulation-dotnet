// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when a device fails to send a messages due to a IO exception.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class TelemetrySendIOException : CustomException
    {
        public TelemetrySendIOException(string message) : base(message)
        {
        }

        public TelemetrySendIOException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
