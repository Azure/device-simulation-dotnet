// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when a client attempts to connect and IoT Hub refuses
    /// the connection due to incorrect credentials.
    /// </summary>
    public class DeviceAuthFailedException : CustomException
    {
        public DeviceAuthFailedException() : base()
        {
        }

        public DeviceAuthFailedException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }

        public DeviceAuthFailedException(string message) : base(message)
        {
        }

        public DeviceAuthFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
