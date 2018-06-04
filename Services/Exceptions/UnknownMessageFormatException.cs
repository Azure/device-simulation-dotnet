// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when a client is requesting a resource that
    /// doesn't exist yet.
    /// </summary>
    public class UnknownMessageFormatException : Exception
    {
        public UnknownMessageFormatException() : base()
        {
        }

        public UnknownMessageFormatException(string message) : base(message)
        {
        }

        public UnknownMessageFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
